var vueStatsDefinition = {
    template: null,
    props: {
        show: Boolean,
        data: Object
    },
    methods: {
        onSubmitDoNothing: function () {
        },
        close: function () {
            // Some save logic goes here...
            this.$emit('close');
        }
    }

};

function vueStatsResolver(resolve, reject) {
    axios.get('/comp/modal_stats.html')
    .then(response => {
        vueStatsDefinition.template = response.data;
        resolve(vueStatsDefinition);
    });
}

Vue.component('modal_stats', vueStatsResolver);
