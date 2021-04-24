var router = new VueRouter({
    mode: 'history',
    routes: []
});


var visCharting = new Vue({
    router,
    el: '#vis_charting',
    data: {
        mangaName: "",
    },

    mounted: function () {
        document.onreadystatechange = () => {
            if (document.readyState == "complete") {
                renderWindowData(this.$route.query.id, this.renderChart);
            }
        }
    },
    methods: {
        renderChart: function (data) {
            var myChart = new Chart(document.getElementById("vis-charting"), {
                type: data.chartData.chart_type,
                data: { labels: data.chartData.labels, datasets: data.chartData.datasets },
                options: data.chartData.chart_options,
            });
        }
    }
})
