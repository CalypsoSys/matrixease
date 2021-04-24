var router = new VueRouter({
    mode: 'history',
    routes: []
});


var visReporting = new Vue({
    router,
    el: '#vis_reporting',
    data: {
        mangaName: "",
    },

    mounted: function () {
        document.onreadystatechange = () => {
            if (document.readyState == "complete") {
                renderWindowData(this.$route.query.id, this.renderReport);
            }
        }
    },
    methods: {
        renderReport: function (data) {
            var myReport = new gridjs.Grid({
                columns: data.reportData.columns,
                search: true,
                sort: true,
                pagination: true,
                fixedHeader: true,
                height: '400px',
                autoWidth: true,
                data: data.reportData.data
            }).render(document.getElementById("vis-reporting"));
        }
    }
})
