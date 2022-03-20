var vueAuthDefinition =
{
    template: null,
    props: {
        show: Boolean,
    },
    data: function () {
        return {
            matrixEaseKey: "",
            emailAddress: "",
            myCaptcha: null,
            validCapcha: false,
            captchaValue: "",
            emailCode: "",
            showModalAlert: false,
            modal_title: "",
            modal_message: "",
            modal_secondary: "",
            modal_list: [],
            modal_yes_no_action: "",
        }
    },
    mounted: function () {
        this.matrixEaseKey = document.getElementById('matrixease_id').value;
        this.jCaptchaInit();
    },
    methods: {
        onSubmitDoNothing: function () {

        },
        submitGoogle: function () {
            this.$refs.form.submit()
        },
        inValidateCaptchaCode: function () {
            return !this.emailAddress || !this.captchaValue;
        },
        validateCaptcha: function () {
            this.myCaptcha.validate();
        },
        inValidateEmailCode: function () {
            return !this.emailAddress || !this.validCapcha || !this.emailCode;
        },
        sendEmailCode: function (code, parts) {
            axios.get('/send_email_code', {
                params: {
                    matrixease_id: document.getElementById('matrixease_id').value,
                    email_to_address: this.emailAddress,
                    result: parts
                }
            })
                .then(response => {
                    if (!response || !response.data || response.data.Success != true) {
                        this.validCapcha = false;
                        this.captchaValue = "";
                        this.emailCode = "";
                        this.myCaptcha.reset();
                        mangaAuth.showModalDialog("Error", "Vis: could not send email access code");
                    }
                })
                .catch(error => {
                    this.showModalDialog("Error", "Vis: unknown error send email access code");
                });
        },
        validateEmailCode: function (response, $captchaInputElement, numberOfTries) {
            axios.get('/validate_email_code', {
                params: {
                    matrixease_id: document.getElementById('matrixease_id').value,
                    email_to_address: this.emailAddress,
                    emailed_code: this.emailCode
                }
            })
            .then(response => {
                if (!response || !response.data || response.data.Success != true) {
                    this.validCapcha = false;
                    this.captchaValue = "";
                    this.emailCode = "";
                    this.myCaptcha.reset();
                    mangaAuth.showModalDialog("Error", "Vis: could not validate email access code");
                } else {
                    this.$emit('close');
                }
            })
            .catch(error => {
                this.showModalDialog("Error", "Vis: unknown error validating email access code");
            })
        },
        jCaptchaInit: function () {
            // captcha initial setup
            this.myCaptcha = new jCaptcha({
                myVue: this,
                el: '.jCaptcha',
                canvasClass: 'jCaptchaCanvas',
                canvasStyle: {
                    // properties for captcha stylings
                    width: 100,
                    height: 15,
                    textBaseline: 'top',
                    font: '15px Arial',
                    textAlign: 'left',
                    fillStyle: "#000"
                },

                // set callback function
                callback: function (response, $captchaInputElement, numberOfTries, parts) {

                    if (maxNumberOfTries === numberOfTries) {
                        this.myVue.emailAddress = "";
                        this.myVue.validCapcha = false;
                        this.myVue.captchaValue = "";
                        this.myVue.emailCode = "";
                    } else if (response == 'success') {
                        this.myVue.validCapcha = true;
                        this.myVue.sendEmailCode(this.myVue.captchaValue, parts);
                    } else if (response == 'error') {
                        this.myVue.validCapcha = false;
                        this.myVue.captchaValue = "";
                        this.myVue.emailCode = "";
                    }
                }
            });
        }
    },
};

function vueAuthResolver(resolve, reject) {
    axios.get('/comp/modal_login.html')
    .then(response => {
        vueAuthDefinition.template = response.data;
        resolve(vueAuthDefinition);
    });
}

Vue.component('modal_login', vueAuthResolver);
