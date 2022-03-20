var vueSettingsDefinition = {
    name: "left_slide_panel",
    template: null,
    props: {
        cur_show_low_equal: Boolean,
        cur_show_low_bound: Number,
        cur_show_high_equal: Boolean,
        cur_show_high_bound: Number,
        cur_show_percentage: String,
        cur_select_operation: String,
        cur_col_ascending: Boolean,
        cur_hide_columns: Array,
        cur_columns: Array
    },
    data() {
        return {
            show_low_equal: this.cur_show_low_equal,
            show_low_bound: this.cur_show_low_bound,
            show_high_equal: this.cur_show_high_equal,
            show_high_bound: this.cur_show_high_bound,
            show_percentage: this.cur_show_percentage,
            select_operation: this.cur_select_operation,
            col_ascending: this.cur_col_ascending,
            hide_columns: [...this.cur_hide_columns]
        };
    },
    methods: {
        closePanel() {
            if (this.show_low_bound <= this.show_high_bound) {
                this.$emit("closePanel", {
                    show_low_equal: this.show_low_equal,
                    show_low_bound: this.show_low_bound,
                    show_high_equal: this.show_high_equal,
                    show_high_bound: this.show_high_bound,
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
