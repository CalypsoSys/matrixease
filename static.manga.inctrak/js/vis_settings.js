var vueSettingsDefinition = {
    name: "left_slide_panel",
    template: null,
    props: {
        cur_show_ge_selected: Number,
        cur_show_le_selected: Number,
        cur_show_percentage: String,
        cur_select_operation: String,
        cur_col_ascending: Boolean,
        cur_hide_columns: Array,
        cur_columns: Array
    },
    data() {
        return {
            show_ge_selected: this.cur_show_ge_selected,
            show_le_selected: this.cur_show_le_selected,
            show_percentage: this.cur_show_percentage,
            select_operation: this.cur_select_operation,
            col_ascending: this.cur_col_ascending,
            hide_columns: [...this.cur_hide_columns]
        };
    },
    methods: {
        closePanel() {
            if (this.show_ge_selected <= this.show_le_selected) {
                this.$emit("closePanel", {
                    show_ge_selected: this.show_ge_selected,
                    show_le_selected: this.show_le_selected,
                    show_percentage: this.show_percentage,
                    select_operation: this.select_operation,
                    col_ascending: this.col_ascending,
                    hide_columns: this.hide_columns
                });
            }
        }
    }
};


function vueSettingsResolver(resolve, reject) {
    axios.get('/comp/modal_settings.html')
        .then(response => {
            vueSettingsDefinition.template = response.data;
            resolve(vueSettingsDefinition);
        });
}

Vue.component('left_slide_panel', vueSettingsResolver);
