import { apiGet } from '@/services/api'

export type MatrixEaseProject = {
  ProjectId: string
  Name: string
  OriginalName: string
  SheetType: string
  Created: string
  MaxRows: number
  TotalRows: number | null
  Status: string
  IsPending: boolean
}

export type MatrixEaseProjectsResponse = {
  Success: boolean
  Message?: string
  Projects: MatrixEaseProject[]
}

export function fetchProjects(): Promise<MatrixEaseProjectsResponse> {
  return apiGet<MatrixEaseProjectsResponse>('/api/matrixease/projects')
}
