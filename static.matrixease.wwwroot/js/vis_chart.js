var vueChartDefinition = {
    template: null,
    props: {
        show: Boolean,
        dimension_columns: Array,
        measure_columns: Array
    },
    data: function () {
        return {
            showChart: false,
            showReport: false,

            chartCTX: null,
            included_dim: 0,
            included_dimension: [],
            included_measure_total: [],
            included_measure_average: [],
            included_measure_count: [],
            included_measure_wtf: [],
            chart_type: "line",
            filtered: true,
            myChart: null,

            chart_options: {
                responsive: true,
                lineTension: 1,
                scales: {
                    yAxes: [{
                        ticks: {
                            beginAtZero: true,
                            padding: 25,
                        }
                    }],
                    xAxes: [{
                        ticks: {
                            autoSkip: false
                        }
                    }]
                }
            },
            chartData: {},
            myReport: null,
            reportData: {}
        }
    },
    mounted() {
        this.chartCTX = document.getElementById("vis-quick-chart");
    },
    methods: {
        getRandomColor: function () {
            var letters = '0123456789ABCDEF'.split('');
            var color = '#';
            for (var i = 0; i < 6; i++) {
                color += letters[Math.floor(Math.random() * 16)];
            }
            return color;
        },
        onSubmitDoNothing: function () {
        },
        getSelectedCols: function (colArray, notThis) {
            var colIndexes = [];
            for (var i = 0; i < colArray.length; i++) {
                if (colArray[i]) {
                    if (i == notThis) {
                        return null;
                    } else {
                        colIndexes.push(i);
                    }
                }
            }

            return colIndexes;
        },
        createChart: function () {
            //var dimensionIndexes = this.getSelectedCols(this.included_dimension);
            var dimensionIndexes = [this.included_dim];
            var measureTotalIndexes = this.getSelectedCols(this.included_measure_total, this.included_dim);
            var measureAvgIndexes = this.getSelectedCols(this.included_measure_average, this.included_dim);
            var measureCountIndexes = this.getSelectedCols(this.included_measure_count, this.included_dim);

            if (measureTotalIndexes == null || measureAvgIndexes === null || measureCountIndexes == null) {
                visualizer.showModalDialog("Attention", "Dimension cannot also be measure.");
                return;
            } else if (dimensionIndexes.length == 0 || (measureTotalIndexes.length + measureAvgIndexes.length + measureCountIndexes.length) == 0) {
                visualizer.showModalDialog("Attention", "Please select at least one dimension and one measure.");
                return;
            }
            //
            axios.get('/api/visualize/get_chart_data', {
                params: {
                    matrixease_id: document.getElementById('matrixease_id').value,
                    vis_id: visualizer.vis_id,
                    chart_type: this.chart_type,
                    col_dimension_indexes: dimensionIndexes.join(","),
                    col_measure_tot_indexes: measureTotalIndexes.join(","),
                    col_measure_avg_indexes: measureAvgIndexes.join(","),
                    col_measure_cnt_indexes: measureCountIndexes.join(","),
                    filtered: this.filtered
                }
            })
            .then(response => {
                if (response.data && response.data.Success && response.data.ChartData && this.chart_type != "report") {
                    this.showChart = true;
                    this.showReport = false;
                    if (this.myChart) {
                        this.myChart.destroy();
                    }
                    this.chartData = response.data.ChartData;
                    this.myChart = new Chart(this.chartCTX, {
                        type: this.chart_type,
                        data: response.data.ChartData,
                        options: this.chart_options,
                    });
                } else if (response.data && response.data.Success && response.data.ReportData && this.chart_type == "report") {
                    this.showChart = false;
                    this.showReport = true;
                    this.reportData = response.data.ReportData;
                    if (this.myReport == null) {
                        response.data.ReportData.search = true;
                        response.data.ReportData.sort = true;
                        response.data.ReportData.pagination = true;
                        response.data.ReportData.fixedHeader = true;
                        response.data.ReportData.height = '400px';
                        this.myReport = new gridjs.Grid(response.data.ReportData).render(document.getElementById("vis-quick-report"));
                    } else {
                        this.myReport.updateConfig({
                            columns: response.data.ReportData.columns,
                            data: response.data.ReportData.data
                        }).forceRender();
                    }
                } else {
                    visualizer.showModalDialog("Unknown Error", "Vis: failure getting chart data.");
                }
            })
            .catch(error => {
                visualizer.showModalDialog("Unknown Error", "Vis: unknown error chart data.", error);
            });
        },
        openReportWindow: function () {
            if (this.chart_type == "report") {
                var visReporting = customWindowOpen("/matrixease_reports.html", "_blank",
                    { reportData:
                        {
                            //report_name: JSON.stringify(this.reportData.report_name),
                            columns: this.reportData.columns,
                            data: this.reportData.data
                        }
                });
                /*
                visReporting.reportData = {
                    //report_name: JSON.stringify(this.reportData.report_name),
                    columns: JSON.stringify(this.reportData.columns),
                    data: JSON.stringify(this.reportData.data)
                };
                */
            } else {
                var dataSets = [];
                for (var i = 0; i < this.chartData.datasets.length; i++) {
                    dataSets.push({
                        backgroundColor: this.chartData.datasets[i].backgroundColor,
                        borderWidth: this.chartData.datasets[i].borderWidth,
                        data: this.chartData.datasets[i].data,
                        label: this.chartData.datasets[i].label,
                    });
                }
                var vischarting = customWindowOpen("/matrixease_charts.html", "_blank",
                    { chartData:
                        {
                            chart_type: this.chart_type,
                            labels: this.chartData.labels,
                            datasets: dataSets,
                            chart_options: this.chart_options
                        }
                });
                /*
                vischarting.chartData = {
                    chart_type: JSON.stringify(this.chart_type),
                    labels: JSON.stringify(this.chartData.labels),
                    datasets: JSON.stringify(dataSets),
                    chart_options: JSON.stringify(this.chart_options)
                };*/
            }
        },
        closeUp: function () {
            this.$emit('close');
        }
    }
};

function vueChartResolver(resolve, reject) {
    axios.get('/comp/modal_chart.html')
    .then(response => {
        vueChartDefinition.template = response.data;
        resolve(vueChartDefinition);
    });
}

Vue.component('modal_chart', vueChartResolver);
