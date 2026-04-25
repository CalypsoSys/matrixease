import { describe, expect, it } from 'vitest'
import { buildApiUrl } from '@/services/api'
import { createDefaultUploadForm, summarizePayload, validateUploadForm } from '@/utils/matrix'

describe('validateUploadForm', () =>
{
  it('requires a project name', () =>
  {
    const form = createDefaultUploadForm()
    expect(validateUploadForm(form)).toContain('MatrixEase name is required.')
  })

  it('rejects inconsistent header settings', () =>
  {
    const form = {
      ...createDefaultUploadForm(),
      mangaName: 'Orders',
      headerRow: 2,
      headerRows: 1
    }

    expect(validateUploadForm(form)).toContain('Header on row cannot be greater than header rows.')
  })
})

describe('summarizePayload', () =>
{
  it('counts total, measure, and bucketized columns', () =>
  {
    const summary = summarizePayload({
      TotalRows: 10,
      SelectedRows: 5,
      Columns: {
        Region: {
          Index: 0,
          ColType: 'Dimension',
          DataType: 'String',
          NullEmpty: 0,
          Selectivity: 1,
          DistinctValues: 2,
          Bucketized: false,
          OnlyBuckets: false,
          CurBucketSize: 0,
          MinBucketSize: 0,
          CurBucketMod: 0,
          MinBucketMod: 0,
          AllowedBuckets: [],
          Attributes: null,
          Values: []
        },
        Amount: {
          Index: 1,
          ColType: 'Measure',
          DataType: 'Number',
          NullEmpty: 0,
          Selectivity: 1,
          DistinctValues: 10,
          Bucketized: true,
          OnlyBuckets: false,
          CurBucketSize: 5,
          MinBucketSize: 1,
          CurBucketMod: 0,
          MinBucketMod: 0,
          AllowedBuckets: [],
          Attributes: null,
          Values: []
        }
      },
      ShowLowEqual: true,
      ShowLowBound: 0,
      ShowHighEqual: true,
      ShowHighBound: 100,
      ShowPercentage: 'pct_tot_sel',
      SelectOperation: 'overwrite_selection',
      SelectionExpression: null,
      ColAscending: false,
      HideColumns: []
    })

    expect(summary).toEqual({
      columnCount: 2,
      measureCount: 1,
      bucketizedCount: 1
    })
  })
})

describe('buildApiUrl', () =>
{
  it('resolves api paths against the runtime base URL', () =>
  {
    expect(buildApiUrl('https://api.matrixease.com', '/api/session/bootstrap')).toBe(
      'https://api.matrixease.com/api/session/bootstrap'
    )
  })
})
