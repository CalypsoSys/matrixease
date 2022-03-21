var router = new VueRouter({
    mode: 'history',
    routes: []
});


var mxesDependencies = new Vue({
    router,
    el: '#mxes_dependencies',
    data: {
        mangaName: "",
        curdepdata: null,
        colData: null,
    },

    mounted: function () {
        document.onreadystatechange = () => {
            if (document.readyState == "complete") {
                renderWindowData(this.$route.query.id, this.renderChord);
            }
        }
    },
    methods: {
        onChange() {
            initChords();
            updateChords(this.colData.keys, this.colData.matrix);
        },
        renderChord: function (data) {
            this.curdepdata = data.depData.curdepdata;
        }
    }
})
