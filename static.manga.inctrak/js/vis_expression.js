var vueExpressionDefinition = {
    template: null,
    props: {
        show: Boolean,
        cur_selection_expression: String,
        cur_saved_expression: String
    },
    data: function () {
        return {
            dialog: null,
            selection_expression: this.cur_selection_expression,
            saved_expression: this.cur_saved_expression
        }
    },
    watch: {
        cur_selection_expression: function (selection_expression) {
            this.selection_expression = selection_expression;
        },
        cur_saved_expression: function (saved_expression) {
            this.saved_expression = saved_expression;
        },
        show: function (show) {
            this.dialog.showDialog();
        }
    },
    mounted: function () {
        this.dialog = new DialogBox("expression_dialog", function () { });
        // Show Dialog Box
        this.dialog.showDialog();
    },
    methods: {
        onSubmitDoNothing: function () {
        },
        emitUpdate(event) {
            this.$emit('update:cur_selection_expression', this.selection_expression);
        },
        clearExpr: function () {
            this.selection_expression = "";
            this.emitUpdate();
        },
        restoreExpr: function () {
            this.selection_expression = this.saved_expression;
            this.emitUpdate();
        },
        groupExpr: function () {
            this.selection_expression = "(" + this.selection_expression + ")";
            this.emitUpdate();
        },
        notExpr: function () {
            this.selection_expression = "NOT (" + this.selection_expression + ")";
            this.emitUpdate();
        },
        filterByNode: function () {
            axios.get('/api/visualize/filter', {
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value,
                    vis_id: visualizer.vis_id,
                    selection_expression: this.selection_expression
                }
            })
            .then(response => {
                if (!response.data) {
                    visualizer.showModalDialog("Error", "Vis: no filter data.", "");
                } else {
                    visualizer.showModalStatusDialog(response.data.PickupKey, response.data.StatusData);
                }
            })
            .catch(error => {
                this.showModalDialog("Unknown Error", "Vis: unknown error filtering.", error);
            });

            if (event.preventDefault != undefined)
                event.preventDefault();
            if (event.stopPropagation != undefined)
                event.stopPropagation();
        },
    }
};

function vueExpressionResolver(resolve, reject) {
    axios.get('/comp/modal_expression.html')
    .then(response => {
        vueExpressionDefinition.template = response.data;
        resolve(vueExpressionDefinition);
    });
}

Vue.component('modal_expression', vueExpressionResolver);
