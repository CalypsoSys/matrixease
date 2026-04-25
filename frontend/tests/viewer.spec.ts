import { describe, expect, it } from 'vitest'
import { appendSelectionExpression, buildColumnEntries, buildSelectionExpression, createViewerBucketForm, createViewerSettings, renderedValuesForColumn, syncBucketForm, toggleIndexSelection } from '@/utils/viewer'

describe('viewer utils', () =>
{
  it('builds ordered column entries', () =>
  {
    const entries = buildColumnEntries({
      TotalRows: 10,
      SelectedRows: 2,
      Columns: {
        B: { Index: 1, ColType: 'Dimension', DataType: 'String', NullEmpty: 0, Selectivity: 1, DistinctValues: 1, Bucketized: false, OnlyBuckets: false, CurBucketSize: 0, MinBucketSize: 0, CurBucketMod: 0, MinBucketMod: 0, AllowedBuckets: [], Attributes: null, Values: [] },
        A: { Index: 0, ColType: 'Measure', DataType: 'Number', NullEmpty: 0, Selectivity: 1, DistinctValues: 1, Bucketized: false, OnlyBuckets: false, CurBucketSize: 0, MinBucketSize: 0, CurBucketMod: 0, MinBucketMod: 0, AllowedBuckets: [], Attributes: null, Values: [] }
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

    expect(entries.map(entry => entry.name)).toEqual(['A', 'B'])
  })

  it('appends expressions using logical operators', () =>
  {
    expect(appendSelectionExpression('"A"', '"B"', 'and_selections')).toBe('"A" AND "B"')
    expect(appendSelectionExpression('"A"', '"B"', 'or_selection')).toBe('"A" OR "B"')
    expect(appendSelectionExpression('', '"B"', 'overwrite_selection')).toBe('"B"')
  })

  it('builds a selection expression for a chosen value', () =>
  {
    const column = {
      name: 'Region',
      Index: 2,
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
    }

    expect(buildSelectionExpression(column, {
      ColumnValue: 'North',
      Duplicates: 0,
      TotalPct: 10,
      SelectAllPct: 20,
      SelectRelPct: 30,
      TotalValues: 4,
      SelectedValues: 2
    })).toBe('"North@Region:2"')
  })

  it('filters rendered values using the viewer settings thresholds', () =>
  {
    const settings = createViewerSettings()
    settings.showLowBound = 10
    settings.showHighBound = 60

    const values = renderedValuesForColumn({
      Index: 0,
      ColType: 'Dimension',
      DataType: 'String',
      NullEmpty: 0,
      Selectivity: 1,
      DistinctValues: 3,
      Bucketized: false,
      OnlyBuckets: false,
      CurBucketSize: 0,
      MinBucketSize: 0,
      CurBucketMod: 0,
      MinBucketMod: 0,
      AllowedBuckets: [],
      Attributes: null,
      Values: [
        { ColumnValue: 'Low', Duplicates: 0, TotalPct: 2, SelectAllPct: 2, SelectRelPct: 5, TotalValues: 1, SelectedValues: 1 },
        { ColumnValue: 'Mid', Duplicates: 0, TotalPct: 22, SelectAllPct: 22, SelectRelPct: 25, TotalValues: 3, SelectedValues: 3 },
        { ColumnValue: 'High', Duplicates: 0, TotalPct: 80, SelectAllPct: 80, SelectRelPct: 80, TotalValues: 7, SelectedValues: 7 }
      ]
    }, settings)

    expect(values.map(value => value.ColumnValue)).toEqual(['Mid'])
  })

  it('toggles selected indexes in place', () =>
  {
    const indexes = [1, 2]
    toggleIndexSelection(indexes, 2)
    toggleIndexSelection(indexes, 5)
    expect(indexes).toEqual([1, 5])
  })

  it('syncs bucket form from the selected column', () =>
  {
    const bucketForm = createViewerBucketForm()
    syncBucketForm({
      name: 'Amount',
      Index: 1,
      ColType: 'Measure',
      DataType: 'Number',
      NullEmpty: 0,
      Selectivity: 1,
      DistinctValues: 10,
      Bucketized: true,
      OnlyBuckets: false,
      CurBucketSize: 25,
      MinBucketSize: 1,
      CurBucketMod: 5,
      MinBucketMod: 0,
      AllowedBuckets: [],
      Attributes: null,
      Values: []
    }, bucketForm)

    expect(bucketForm).toEqual({
      bucketized: true,
      bucketSize: 25,
      bucketMod: 5
    })
  })
})
