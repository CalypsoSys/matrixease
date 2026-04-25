import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { createMatrixEaseApi, type MatrixEaseCatalogEntry, type MatrixEaseJobStartResponse } from '@/services/api'
import { useRuntimeStore } from '@/stores/runtime'
import { useSessionStore } from '@/stores/session'
import { createDefaultUploadForm, validateUploadForm } from '@/utils/matrix'

export function useDashboard()
{
  const router = useRouter()
  const runtimeStore = useRuntimeStore()
  const session = useSessionStore()
  const api = createMatrixEaseApi(runtimeStore.runtime)

  const loading = ref(true)
  const loadingProjects = ref(false)
  const uploading = ref(false)
  const checkingGoogle = ref(false)
  const uploadProgress = ref(0)
  const projects = ref<MatrixEaseCatalogEntry[]>([])
  const errorMessage = ref('')
  const statusMessage = ref('')
  const authBusy = ref(false)
  const emailAddress = ref('')
  const captchaPrompt = ref('')
  const captchaOperands = ref<{ num1: number, num2: number } | null>(null)
  const captchaAnswer = ref('')
  const emailCode = ref('')
  const selectedFile = ref<File | null>(null)
  const form = ref(createDefaultUploadForm())

  const readyForCatalog = computed(() => session.hasAcceptedCookies && session.hasAccessToken)
  const validationErrors = computed(() => validateUploadForm(form.value))

  const navigateToViewer = async (mxesId: string) =>
  {
    await router.push({ name: 'viewer', params: { mxesId } })
  }

  const loadProjects = async () =>
  {
    if (!readyForCatalog.value || !session.matrixEaseId) {
      projects.value = []
      return
    }

    loadingProjects.value = true
    errorMessage.value = ''

    try {
      const response = await api.getMyMangas(session.matrixEaseId)
      projects.value = response.Success ? response.MyMangas : []

      if (!response.Success) {
        errorMessage.value = 'The API did not return a project catalog.'
      }
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to load projects.'
    } finally {
      loadingProjects.value = false
    }
  }

  const awaitJobCompletion = async (response: MatrixEaseJobStartResponse) =>
  {
    if (!response.MatrixId || !session.matrixEaseId) {
      throw new Error('The API did not return a matrix identifier.')
    }

    for (let attempt = 0; attempt < 120; attempt += 1) {
      const status = await api.getMangaStatus(session.matrixEaseId, response.MatrixId)

      if (!status.Success) {
        throw new Error(status.Message ?? 'Matrix job status failed.')
      }

      if (status.Complete) {
        await loadProjects()
        await navigateToViewer(response.MatrixId)
        return
      }

      await new Promise(resolve => setTimeout(resolve, 1000))
    }

    throw new Error('Timed out waiting for MatrixEase processing to finish.')
  }

  const initialize = async () =>
  {
    loading.value = true
    errorMessage.value = ''

    try {
      await session.refreshBootstrap(api)
      await refreshCaptcha()

      if (readyForCatalog.value) {
        const hasValidAccess = await session.validateAccess(api)
        if (hasValidAccess) {
          await loadProjects()
        }
      }
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to initialize session.'
    } finally {
      loading.value = false
    }
  }

  const refreshCaptcha = async () =>
  {
    if (!session.matrixEaseId) {
      return
    }

    try {
      const captcha = await api.getCaptcha(session.matrixEaseId)
      captchaOperands.value = captcha
      captchaPrompt.value = `${captcha.num1} + ${captcha.num2} =`
    } catch {
      captchaOperands.value = null
      captchaPrompt.value = 'Captcha unavailable'
    }
  }

  const acceptCookies = () =>
  {
    session.acceptCookies()
    void initialize()
  }

  const sendEmailCode = async () =>
  {
    if (!session.matrixEaseId || !captchaOperands.value || !emailAddress.value.trim()) {
      errorMessage.value = 'Enter an email address and captcha result before requesting a code.'
      return
    }

    authBusy.value = true
    errorMessage.value = ''
    statusMessage.value = ''

    try {
      const result = `${captchaOperands.value.num1},${captchaOperands.value.num2},${captchaAnswer.value.trim()}`
      const response = await api.sendEmailCode(session.matrixEaseId, emailAddress.value.trim(), result)

      if (!response.Success) {
        throw new Error('The API could not send the email access code.')
      }

      statusMessage.value = 'Email code sent.'
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to send email code.'
    } finally {
      authBusy.value = false
    }
  }

  const validateEmailCode = async () =>
  {
    if (!session.matrixEaseId || !emailAddress.value.trim() || !emailCode.value.trim()) {
      errorMessage.value = 'Enter both the email address and the emailed code.'
      return
    }

    authBusy.value = true
    errorMessage.value = ''
    statusMessage.value = ''

    try {
      const response = await api.validateEmailCode(session.matrixEaseId, emailAddress.value.trim(), emailCode.value.trim())
      if (!response.Success) {
        throw new Error('The email access code was not accepted.')
      }

      await session.refreshBootstrap(api)
      const validAccess = await session.validateAccess(api)
      if (!validAccess) {
        throw new Error('Access cookie was set, but validation did not succeed.')
      }

      statusMessage.value = 'Access granted.'
      await loadProjects()
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to validate email code.'
      await refreshCaptcha()
    } finally {
      authBusy.value = false
    }
  }

  const startGoogleLogin = () =>
  {
    if (!session.matrixEaseId) {
      return
    }

    window.location.assign(`/google/login?matrixease_id=${encodeURIComponent(session.matrixEaseId)}`)
  }

  const updateEmailAddress = (value: string) =>
  {
    emailAddress.value = value
  }

  const updateCaptchaAnswer = (value: string) =>
  {
    captchaAnswer.value = value
  }

  const updateEmailCode = (value: string) =>
  {
    emailCode.value = value
  }

  const onFileSelected = (event: Event) =>
  {
    const input = event.target as HTMLInputElement
    selectedFile.value = input.files?.[0] ?? null
  }

  const submitUpload = async () =>
  {
    if (!selectedFile.value) {
      errorMessage.value = 'Choose a CSV or spreadsheet file before uploading.'
      return
    }

    if (validationErrors.value.length > 0 || !session.matrixEaseId) {
      errorMessage.value = validationErrors.value.join(' ')
      return
    }

    uploading.value = true
    uploadProgress.value = 0
    errorMessage.value = ''
    statusMessage.value = ''

    try {
      const response = await api.uploadSheet({
        ...form.value,
        file: selectedFile.value,
        matrixEaseId: session.matrixEaseId
      }, value => {
        uploadProgress.value = value
      })

      if (!response.Success) {
        errorMessage.value = response.Error ?? 'Upload failed.'
        return
      }

      statusMessage.value = 'Upload accepted. Waiting for processing to complete.'
      await awaitJobCompletion(response)
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Upload failed.'
    } finally {
      uploading.value = false
    }
  }

  const submitGoogleSheet = async () =>
  {
    if (validationErrors.value.length > 0 || !session.matrixEaseId) {
      errorMessage.value = validationErrors.value.join(' ')
      return
    }

    checkingGoogle.value = true
    errorMessage.value = ''
    statusMessage.value = ''

    try {
      const googleStatus = await api.checkGoogleLogin(session.matrixEaseId)
      if (!googleStatus.Success) {
        errorMessage.value = 'Google authentication is required before importing a sheet.'
        return
      }

      const response = await api.submitGoogleSheet({
        ...form.value,
        matrixEaseId: session.matrixEaseId
      })

      if (!response.Success) {
        errorMessage.value = response.Error ?? 'Google Sheet import failed.'
        return
      }

      statusMessage.value = 'Google Sheet import accepted. Waiting for processing to complete.'
      await awaitJobCompletion(response)
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Google Sheet import failed.'
    } finally {
      checkingGoogle.value = false
    }
  }

  onMounted(() => {
    void initialize()
  })

  return {
    runtime: runtimeStore.runtime,
    form,
    loading,
    loadingProjects,
    uploading,
    checkingGoogle,
    uploadProgress,
    projects,
    errorMessage,
    statusMessage,
    authBusy,
    emailAddress,
    captchaPrompt,
    captchaAnswer,
    emailCode,
    readyForCatalog,
    cookiesAccepted: computed(() => session.hasAcceptedCookies),
    hasAccessToken: computed(() => session.hasAccessToken),
    googleSignedIn: computed(() => session.googleSignedIn),
    validationErrors,
    acceptCookies,
    sendEmailCode,
    validateEmailCode,
    startGoogleLogin,
    updateEmailAddress,
    updateCaptchaAnswer,
    updateEmailCode,
    onFileSelected,
    submitUpload,
    submitGoogleSheet,
    navigateToViewer,
    loadProjects
  }
}
