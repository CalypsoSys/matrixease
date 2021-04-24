var vueStatusDefinition = {
    template: null,
    props: {
        show: Boolean,
        statusfunc: Function,
        curstatusdata: Object,
    },
    data: function () {
        return {
            statusdata: this.curstatusdata,
            timer: ''
        }
    },
    watch: {
        curstatusdata: function (curstatusdata) {
            this.curstatusdata = curstatusdata;
            this.statusdata = curstatusdata;
        },
    },
    created: function () {
      this.timer = setInterval(this.fetchEventsList, 5000);
    },
    methods: {
        fetchEventsList() {
            this.statusfunc();
        },
        cancelAutoUpdate() {
            clearInterval(this.timer)
        },
        onSubmitDoNothing: function () {
        },
        cancel: function () {
            // Some save logic goes here...
            this.$emit('close');
            this.$emit('cancel', true);
        }
    }
};

function vueStatusResolver(resolve, reject) {
    axios.get('/comp/modal_status.html')
    .then(response => {
        vueStatusDefinition.template = response.data;
        resolve(vueStatusDefinition);
    });
}

Vue.component('modal_status', vueStatusResolver);
