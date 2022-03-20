var vueReportDefinition = {
    template: null,
    props: {
        show: Boolean,
        curreportdata: Object,
    },
    data: function () {
        return {
            myReport: null
        }
    },
    watch: {
        curreportdata: function (curreportdata) {
            this.curreportdata = curreportdata;
            if (this.myReport == null) {
                this.myReport = new gridjs.Grid({
                    columns: this.curreportdata.columns,
                    search: true,
                    sort: true,
                    pagination: true,
                    fixedHeader: true,
                    height: '400px',
                    data: this.curreportdata.data
                }).render(document.getElementById("vis-col-report"));
            } else {
                this.myReport.updateConfig({
                    columns: this.curreportdata.columns,
                    data: this.curreportdata.data
                }).forceRender();
            }
        },
    },
    methods: {
        onSubmitDoNothing: function () {
        },
        openColReportWindow: function () {
            var visReporting = customWindowOpen("/mxes_reports.html", "_blank",
                { reportData:
                    {
                        report_name: this.curreportdata.report_name,
                        columns: this.curreportdata.columns,
                        data: this.curreportdata.data
                    }
            });
            /*
            visReporting.reportData = {
                report_name: JSON.stringify(this.curreportdata.report_name),
                columns: JSON.stringify(this.curreportdata.columns),
                data: JSON.stringify(this.curreportdata.data)
            };
            */
        },
        cancel: function () {
            // Some save logic goes here...
            this.$emit('close');
        }
    }
};

function vueReportResolver(resolve, reject) {
    axios.get('/comp/modal_report.html')
    .then(response => {
        vueReportDefinition.template = response.data;
        resolve(vueReportDefinition);
    });
}

Vue.component('modal_report', vueReportResolver);
