var vueMeasuresDefinition = {
    template: null,
    props: {
        show: Boolean,
        selected_column: Object,
        selected_node: Object,
        measure_columns: Array
    },
    data: function () {
        return {
            included_measure: [],
            filtered: true,
            measures: null
        }
    },
    watch: {
        selected_column: function (selected_column) {
            this.selected_column = selected_column;
        },
        selected_node: function (selected_node) {
            this.selected_node = selected_node;
        },
    },
    methods: {
        onSubmitDoNothing: function () {
        },
        getMeasures: function () {
            var measureIndexes = [];
            for (var i = 0; i < this.included_measure.length; i++) {
                if (this.included_measure[i]) {
                    measureIndexes.push(i);
                }
            }
            //
            axios.get('/api/matrixease/get_col_measures', {
                params: {
                    matrixease_id: document.getElementById('matrixease_id').value,
                    mxes_id: matrixease.mxes_id,
                    col_index: this.selected_column.index,
                    selected_node: this.selected_node.value + "@" + this.selected_column.name + ":" + this.selected_column.index,
                    col_measure_indexes: measureIndexes.join(","),
                    filtered: this.filtered
                }
            })
            .then(response => {
                if (response.data && response.data.Success) {
                    this.measures = response.data.MeasureStats;
                } else {
                    matrixease.showModalDialog("Unknown Error", "MatrixEase: failure getting column measures.");
                }
            })
            .catch(error => {
                matrixease.showModalDialog("Unknown Error", "MatrixEase: unknown error column measures.", error);
            });
        },
        closeUp: function () {
            this.measures = null;
            this.$emit('close');
        }
    }
};

function vueMeasuresResolver(resolve, reject) {
    axios.get('/comp/modal_measures.html')
    .then(response => {
        vueMeasuresDefinition.template = response.data;
        resolve(vueMeasuresDefinition);
    });
}

Vue.component('modal_measures', vueMeasuresResolver);
