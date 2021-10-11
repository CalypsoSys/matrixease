var router = new VueRouter({
    mode: 'history',
    routes: []
});


var visualizer = new Vue({
    router,
    el: '#visualize',
    data: {
        svgRows: null,
        svgCols: null,
        svgns: "http://www.w3.org/2000/svg",

        vis_id: "",
        mangaName: "",

        //percentOfTotalColor: "#808080",
        //percentOfSelectedTotalColor: "#696969",
        percentOfTotalColor: "green",
        percentOfSelectedTotalColor: "red",

        showModalAlert: false,
        modal_title: "",
        modal_message: "",
        modal_secondary: "",
        modal_list: [],
        modal_yes_no_action: "",

        showModalStatus: false,
        curstatuskey: "",
        curstatusdata: null,
        statusStart: null,
        statusCheckCount: 0,

        showModalStats: false,
        colStatistics: null,

        boxSpace: 10,
        colMargin: "100px",
        colMarginSize: 0,
        colSize: 10,
        maxWidth: 0,
        rowsPadding: "10px",
        data: null,
        dataPositions: [],

        totalRows: 0,
        selectedRows: 0,

        show_low_equal: true,
        show_low_bound: 0,
        show_high_equal: true,
        show_high_bound: 100,
        show_percentage: "pct_tot_sel",
        select_operation: "overwrite_selection",
        col_ascending: false,
        hide_columns: [],
        my_columns: [],
        selectedColumn: {},
        selectedNode: null,
        showModalColumn: false,

        showModalExpression: true,
        selection_expression: null,
        saved_expression: null,

        colMenuTop: 0,
        colMenuLeft: 0,
        colMenuDisplay: "none",
        cellMenuTop: 0,
        cellMenuLeft: 0,
        cellMenuDisplay: "none",

        showModalMeasures: false,
        measureColumns: [],

        dimensionColumns: [],
        showModalChart: false,

        showModalReport: false,
        curreportdata: {},

        showModalDep: false,
        curdepdata: {},
    },

    mounted: function () {
        document.onreadystatechange = () => {
            if (document.readyState == "complete") {
                this.vis_id = this.$route.query.vis_id;
                this.renderVis();
            }
            window.addEventListener('scroll', this.handleScroll);
        }
    },
    computed: {
        colAttributes1: function () {
            return colAttributes1(this.selectedColumn, false);
        },
        colAttributes2: function () {
            return colAttributes2(this.selectedColumn, false);
        },
        colAttributes3: function () {
            return colAttributes3(this.selectedColumn, false);
        },
        colAttributes4: function () {
            return colAttributes4(this.selectedColumn, false);
        },
        colAttributes5: function () {
            return colAttributes5(this.selectedColumn, false);
        },
        colAttributes6: function () {
            return colAttributes6(this.selectedColumn, false);
        }
    },
    methods: {
        showModalDialog: function (title, message, secondary, yesNoAction, list) {
            this.modal_title = title;
            this.modal_message = message;
            this.modal_secondary = secondary;
            this.modal_list = list;
            this.modal_yes_no_action = yesNoAction;
            this.showModalStatus = false;
            this.showModalAlert = true;
        },
        showModalStatusDialog: function (curstatuskey, curstatusdata) {
            this.curstatuskey = curstatuskey;
            this.curstatusdata = curstatusdata;
            this.showModalStatus = true;
        },
        showModalStatsDialog: function () {
            this.showModalStats = true;
        },
        cancelJob: function (cancelIt) {
            if (cancelIt) {
                alert("cancel job");
            }
            this.curstatuskey = "";
            this.curstatusdata = null;
        },
        mangaProcessStatus: function() {
            if (this.curstatuskey && this.curstatuskey != "NOP" && this.showModalStatus) {
                axios.get('/api/visualize/manga_pickup_status/', {
                    params: {
                        inctrak_id: document.getElementById('inctrak_id').value,
                        vis_id: this.vis_id,
                        pickup_key: this.curstatuskey
                    }
                 })
                .then(response => {
                    if (response.data && response.data.Success) {
                        if (response.data.Complete) {
                            this.showModalStatus = false
                            this.curstatusdata = null;
                            this.curstatuskey = "";

                            this.data = response.data.Results;
                            this.drawNodes();
                        } else {
                            this.curstatusdata = response.data.StatusData;
                        }
                    } else {
                        this.showModalDialog("Error", "Vis: failure loading VisAlyzer status.");
                    }
                })
                .catch(error => {
                    this.showModalStatus = false
                    this.showModalDialog("Unknown Error", "Vis: unknown error checking VisAlyzer status.", error);
                });
            }
        },
        renderVis: function () {
            this.showModalStatusDialog("NOP", { "PreProcess": { "Started": new Date(), "Elapsed": "00:00:00", "Desc": "Loading VisAlyzer", "Status": "Starting" } });
            axios.get('/api/visualize', {
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value,
                    vis_id: this.vis_id
                }
            })
            .then(response => {
                this.showModalStatus = false;
                if (!response.data) {
                    this.data = null;
                    this.showModalDialog("Error", "Vis: no data returned", "");
                } else {
                    this.mangaName = response.data.MangaName;
                    this.data = response.data.MangaData;
                    this.drawNodes();
                }
            })
            .catch(error => {
                this.showModalStatus = false;
                this.showModalDialog("Unknown Error", "Vis: unknown error rendering.", error);
            });
        },
        setOptionsFromData: function () {
            if (this.data.ShowLowEqual !== undefined) {
                this.show_low_equal = this.data.ShowLowEqual;
            }
            if (this.data.ShowLowBound !== undefined) {
                this.show_low_bound = this.data.ShowLowBound;
            }
            if (this.data.ShowHighEqual !== undefined) {
                this.show_high_equal = this.data.ShowHighEqual;
            }
            if (this.data.ShowHighBound !== undefined) {
                this.show_high_bound = this.data.ShowHighBound;
            }
            if (this.data.ShowPercentage) {
                this.show_percentage = this.data.ShowPercentage;
            }
            if (this.data.SelectOperation) {
                this.select_operation = this.data.SelectOperation;
            }
            if (this.data.SelectionExpression) {
                this.selection_expression = this.data.SelectionExpression;
                this.saved_expression = this.data.SelectionExpression;
            }
            if (this.data.ColAscending) {
                this.col_ascending = this.data.ColAscending;
            }
            if (this.data.HideColumns) {
                this.hide_columns = this.data.HideColumns;
            }
        },
        startDisplayMode: function (name, width, height) {
            var svg = document.getElementById(name);
            svg.innerHTML = "";
            svg.setAttribute("width", width);
            svg.setAttribute("height", height);

            return svg;
        },
        endDisplayMode: function () {
        },
        nodeSelected: function (event) {
            this.selectedColumn = null;
            this.selectedNode = null;

            var val = event.currentTarget.getAttribute("data-col-node").split(",");
            var colIndex = parseInt(val[0]);

            if (colIndex < this.dataPositions.length) {

                this.selectedColumn = this.dataPositions[colIndex];
                if (val.length > 1) {
                    var nodeIndex = parseInt(val[1]);
                    if (this.selectedColumn && this.selectedColumn.nodes && nodeIndex < this.selectedColumn.nodes.length) {
                        this.selectedNode = this.selectedColumn.nodes[nodeIndex];
                    }
                }
            }
            this.setVisSize();
        },
        nodeAction: function (event) {
            this.nodeSelected(event);
            if (this.selectedColumn && this.selectedNode) {
                this.selectNode(event);
            }
        },
        nodeMenu: function (event) {
            this.nodeSelected(event);
            if (this.selectedColumn) {
                if (this.selectedNode) {
                    this.cellMenuTop = event.pageY-5 + "px";
                    this.cellMenuLeft = event.pageX-5 + "px";
                    this.cellMenuDisplay = "block";
                } else {
                    this.colMenuTop = event.offsetY-5 + "px";
                    this.colMenuLeft = event.offsetX-5 + "px";
                    this.colMenuDisplay = "block";
                }
                
                if (event.preventDefault != undefined)
                    event.preventDefault();
                if (event.stopPropagation != undefined)
                    event.stopPropagation();
                return false;
            }
        },
        addDataListener: function (elm, data) {
            elm.setAttributeNS(null, "data-col-node", data.join());

            elm.addEventListener("click", this.nodeAction);
            elm.addEventListener("contextmenu", this.nodeMenu);
            elm.addEventListener("mousemove", this.nodeSelected);
        },
        addClipPath: function (svg, id, x, y, width, height) {
            var clipPath = document.createElementNS(this.svgns, 'clipPath');
            clipPath.setAttributeNS(null, 'id', id);
            var rect = document.createElementNS(this.svgns, 'rect');
            rect.setAttributeNS(null, 'x', x);
            rect.setAttributeNS(null, 'y', y);
            rect.setAttributeNS(null, 'width', width);
            rect.setAttributeNS(null, 'height', height);
            clipPath.appendChild(rect);
            svg.appendChild(clipPath);
        },
        addRect: function (svg, data, x, y, width, height, color) {
            var rect = document.createElementNS(this.svgns, 'rect');
            rect.setAttributeNS(null, 'x', x);
            rect.setAttributeNS(null, 'y', y);
            rect.setAttributeNS(null, 'width', width);
            rect.setAttributeNS(null, 'height', height);
            rect.setAttributeNS(null, "fill", color);
            rect.setAttributeNS(null, "stroke", "black");
            this.addDataListener(rect, data);
            svg.appendChild(rect);
        },
        addText: function (svg, data, x, y, text, fontSize, bold, clipId) {
            var txt = document.createElementNS(this.svgns, "text");
            txt.setAttributeNS(null, "x", x);
            txt.setAttributeNS(null, "y", y);
            txt.setAttributeNS(null, "font-size", fontSize);
            txt.setAttributeNS(null, "font-family", "Arial");
            if (bold) {
                txt.setAttributeNS(null, "font-weight", "bold");
            }
            txt.setAttributeNS(null, "dominant-baseline", "text-before-edge");
            txt.setAttributeNS(null, "text-anchor", "left");
            txt.setAttributeNS(null, "clip-path", "url(#" + clipId + ")");
            txt.innerHTML = text;
            this.addDataListener(txt, data);
            svg.appendChild(txt);
            /*
            while (txt.getComputedTextLength() > maxLength) {
                text = text.substring(0, text.length - 1)
                txt.innerHTML = text;
            }
            */
        },
        addMenu: function (svg, data, x, y, href) {
            var image = document.createElementNS(this.svgns, "image");
            image.setAttributeNS(null, "x", x);
            image.setAttributeNS(null, "y", y);
            image.setAttributeNS(null, "href", href);
            this.addDataListener(image, data);
            svg.appendChild(image);
        },
        calcTextSizes: function () {
            var vc = document.getElementById("visSizingCanvas");
            vc.width = window.innerWidth;
            vc.height = window.innerHeight;

            var vctx = vc.getContext("2d");
            vctx.clearRect(0, 0, vctx.width, vctx.height);
            vctx.beginPath();
            vctx.font = "bold 18px Arial";
            var textSize = vctx.measureText("M").width;
            textSize = Math.ceil(textSize + (textSize / 3));

            var fudge = textSize / 2;
            var xRectStart = textSize;
            var xTextStart = xRectStart + fudge;
            var yStart = textSize;

            var boxHeight = textSize * 5;
            var boxTextMod = boxHeight / 2;
            vctx.font = "12px Arial";
            var maxWidth = Math.max(boxHeight, vctx.measureText("Selected Pct of Total: 100.00%").width);
            var columns = 1
            var maxRows = 1;

            vctx.font = "18px Arial";
            this.my_columns = [];
            var index = 0;
            for (var colName in this.data.Columns) {
                this.my_columns.push(colName);
                if (!this.hide_columns || this.hide_columns.length == 0 || this.hide_columns[index] != true) {
                    //maxWidth = Math.max(maxWidth, vctx.measureText(colName).width);
                    ++columns;
                    maxRows = Math.max(maxRows, this.data.Columns[colName].Values.length);
                }
                ++index;
            }
            maxWidth += fudge;
            vctx.stroke();

            this.colSize = textSize * 2;
            var maxWdithCols = ((maxWidth * columns) + (columns / 4 * textSize));
            this.maxWidth = maxWdithCols + "px";

            return { maxWdithCols, boxHeight, maxRows, xRectStart, xTextStart, yStart, maxWidth, textSize, boxTextMod, fudge };
        },
        showRelativePct(selectRelPct) {
            if (selectRelPct > this.show_low_bound && selectRelPct <= this.show_high_bound) {
                return true;
            } else if (this.show_low_equal && selectRelPct == this.show_low_bound) {
                return true;
            } else if (this.show_high_equal && selectRelPct == this.show_high_bound) {
                return true;
            }

            return false
        },
        drawNodes: function (ignoreDataOptions) {
            this.setVisSize();
            this.totalRows = this.data.TotalRows;
            this.selectedRows = this.data.SelectedRows;

            if (ignoreDataOptions != true) {
                this.setOptionsFromData();
            }

            var sizer = this.calcTextSizes();
            var svgCols = this.startDisplayMode("visSVGCols", sizer.maxWdithCols, this.colSize);

            var totalHeight = (sizer.boxHeight + this.boxSpace) * (sizer.maxRows + 1);
            var svgRows = this.startDisplayMode("visSVGRows", sizer.maxWdithCols, totalHeight);

            var x = 0;
            this.dataPositions = [];
            index = 0;
            this.dimensionColumns = [];
            this.measureColumns = [];
            for (var colName in this.data.Columns) {
                if (this.hide_columns && this.hide_columns.length != 0 && this.hide_columns[index]) {
                    ++index;
                    continue;
                }
                ++index;

                var col = this.data.Columns[colName];

                if (col.ColType == "Measure") {
                    this.measureColumns.push({ "Name": colName, "Index": col.Index });
                    if (col.DistinctValues <= 100) {
                        this.dimensionColumns.push({ "Name": colName, "Index": col.Index });
                    }
                } else {
                    this.dimensionColumns.push({ "Name": colName, "Index": col.Index });
                }
                 
                var clipId = "clip_" + col.Index;
                
                this.addClipPath(svgCols, clipId + "_C", sizer.xRectStart + x, sizer.yStart, sizer.maxWidth, sizer.textSize);
                var data = [this.dataPositions.length];
                this.addRect(svgCols, data, sizer.xRectStart + x, sizer.yStart, sizer.maxWidth, sizer.textSize, "white");
                this.addText(svgCols, data, sizer.xTextStart + x, sizer.yStart, colName, "18px", true, clipId + "_C");
                this.addMenu(svgCols, data, x + sizer.maxWidth, sizer.yStart + 2, "/img/three-dots-vertical.svg");

                this.addClipPath(svgRows, clipId + "_R", sizer.xRectStart + x, sizer.yStart, sizer.maxWidth, totalHeight)
                var y = 0;
                var nodes = [];
                for (var i = 0; i < col.Values.length; i++) {
                    var colData = col.Values[i];

                    if (this.showRelativePct(colData.SelectRelPct) == true) {

                        var data = [this.dataPositions.length, nodes.length];

                        this.addRect(svgRows, data, sizer.xRectStart + x, y, sizer.maxWidth, sizer.boxHeight, "white");

                        var showPct2 = "";
                        if (this.show_percentage == "pct_tot_sel") {
                            var pctSize = sizer.maxWidth * (colData.TotalPct / 100);
                            this.addRect(svgRows, data, sizer.xRectStart + x, y, pctSize, sizer.boxHeight, this.percentOfTotalColor);
                            showPct2 = "Pct of Total:" + colData.TotalPct.toFixed(2) + "%";
                        }

                        var pctSize;
                        var showPct1 = "";
                        if (this.show_percentage == "pct_of_sel") {
                            pctSize = sizer.maxWidth * (colData.SelectRelPct / 100);
                            showPct1 = "Pct of Selected: " + colData.SelectRelPct.toFixed(2) + "%";
                        } else {
                            pctSize = sizer.maxWidth * (colData.SelectAllPct / 100);
                            showPct1 = "Selected Pct of Total: " + colData.SelectAllPct.toFixed(2) + "%";
                        }
                        this.addRect(svgRows, data, sizer.xRectStart + x, y, pctSize, sizer.boxHeight, this.percentOfSelectedTotalColor);
                        this.addText(svgRows, data, sizer.xTextStart + x, y + sizer.boxTextMod - (sizer.textSize + sizer.textSize / 2), colData.ColumnValue, "18px", false, clipId + "_R");
                        this.addText(svgRows, data, sizer.xTextStart + x, y + sizer.boxTextMod - (sizer.textSize / 2), showPct1, "12px", false, clipId + "_R");
                        this.addText(svgRows, data, sizer.xTextStart + x, y + sizer.boxTextMod + (sizer.textSize / 2), showPct2, "12px", false, clipId + "_R");
                        this.addMenu(svgRows, data, x + sizer.maxWidth, y, "/img/three-dots.svg");

                        nodes.push({
                            "value": colData.ColumnValue, "duplicates": colData.Duplicates, "totalPct": colData.TotalPct.toFixed(4),
                            "selectedAllPct": colData.SelectAllPct.toFixed(4), "selectedRelPct": colData.SelectRelPct.toFixed(4),
                            "totValues": colData.TotalValues.toLocaleString(), "selValues": colData.SelectedValues.toLocaleString(), "rawCol": colData
                        });

                        y += (sizer.boxHeight + this.boxSpace);
                    }
                }

                this.dataPositions.push({
                    "name": colName, "colType": col.ColType, "index": col.Index, "dataType": col.DataType, "nullEmpty": col.NullEmpty,
                    "selectivity": col.Selectivity.toFixed(6), "distinctValues": col.DistinctValues,
                    "onlyBuckets": col.OnlyBuckets, "bucketized": col.Bucketized,
                    "curBucketSize": col.CurBucketSize, "minBucketSize": col.MinBucketSize,
                    "curBucketMod": col.CurBucketMod, "minBucketMod": col.MinBucketMod,
                    "allowedBuckets": col.AllowedBuckets,
                    "attr": col.Attributes, "nodes": nodes
                });

                x += (sizer.maxWidth + sizer.fudge);
            }

            this.endDisplayMode()
            this.setVisSize();
        },
        pendingFilter: function () {
            if ((!this.selection_expression && this.data && this.data.SelectionExpression) || this.selection_expression && this.data && this.selection_expression != this.data.SelectionExpression) {
                return "Pending Filter Change";
            } else {
                return "";
            }
        },
        selectNode: function (event, override) {
            var selectioOperation;
            if (override) {
                selectioOperation = override;
            } else {
                selectioOperation = this.select_operation;
            }
            var expr = "\"" + this.selectedNode.value + "@" + this.selectedColumn.name + ":" + this.selectedColumn.index + "\"";
            if (this.selection_expression && (selectioOperation == "or_selection" || selectioOperation == "and_selections")) {
                var oper;
                if (selectioOperation == "or_selection") {
                    oper = " OR ";
                } else {
                    oper = " AND ";
                }
                this.selection_expression = this.selection_expression + oper + expr;
            } else {
                this.selection_expression = expr;
            }
            this.showFilter();
            //this.filterByNode();
        },
        updateSettings: function (settingsCallback) {
            axios.get('/api/visualize/update_settings', {
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value,
                    vis_id: this.vis_id,
                    show_low_equal: this.show_low_equal,
                    show_low_bound: this.show_low_bound,
                    show_high_equal: this.show_high_equal,
                    show_high_bound: this.show_high_bound,
                    select_operation: this.select_operation,
                    show_percentage: this.show_percentage,
                    col_ascending: this.col_ascending,
                    hide_columns: this.hide_columns.toString()
                }
            })
            .then(response => {
                if (!response.data) {
                    this.showModalDialog("Error", "Vis: no data returned", "");
                } else if (!response.data.Success) {
                    this.showModalDialog("Error", "Vis: could not save settings.", "");
                } else {
                    settingsCallback.call(this, true);
                }
            })
            .catch(error => {
                this.showModalDialog("Unknown Error", "Vis: unknown error saving settings.", error);
            });
        },
        setVisSize: function () {
            var newTop = this.$refs.meta.clientHeight + document.scrollingElement.scrollTop;
            if (newTop > this.colMarginSize) {
                this.colMargin = newTop + "px";
                this.rowsPadding = ((-1 * newTop) + this.$refs.meta.clientHeight) + "px";
                //console.log(this.rowsPadding);
            }
        },
        showPanel: function() {
            const panel = this.$showPanel({
                component: "left_slide_panel",
                cssClass: "left_slide_panel",
                openOn: "left",
                props: {
                    cur_show_low_equal: this.show_low_equal,
                    cur_show_low_bound: this.show_low_bound,
                    cur_show_high_equal: this.show_high_equal,
                    cur_show_high_bound: this.show_high_bound,
                    cur_show_percentage: this.show_percentage,
                    cur_select_operation: this.select_operation,
                    cur_col_ascending: this.col_ascending,
                    cur_hide_columns: this.hide_columns,
                    cur_columns: this.my_columns
                }
            });
            panel.promise.then(result => {
                if (!this || !result)
                    return;
                var order_changed = (this.col_ascending != result.col_ascending);
                var hideColChange = false;
                for (var i = 0; hideColChange == false && i < this.hide_columns.length; i++) {
                    if (this.hide_columns[i] != result.hide_columns[i])
                        hideColChange = true;
                }

                if (this.select_operation != result.select_operation ||
                    this.show_low_equal != result.show_low_equal || this.show_low_bound != result.show_low_bound ||
                    this.show_high_equal != result.show_high_equal || this.show_high_bound != result.show_high_bound ||
                    this.show_percentage != result.show_percentage ||
                    order_changed || hideColChange) {
                    this.select_operation = result.select_operation;
                    this.show_low_equal = result.show_low_equal;
                    this.show_low_bound = result.show_low_bound;
                    this.show_high_equal = result.show_high_equal;
                    this.show_high_bound = result.show_high_bound;
                    this.show_percentage = result.show_percentage;
                    this.col_ascending = result.col_ascending;
                    this.hide_columns = [...result.hide_columns];
                    var settingsCallback = null;
                    if (order_changed) {
                        settingsCallback = this.renderVis;
                    } else {
                        settingsCallback = this.drawNodes;
                    }
                    this.updateSettings(settingsCallback);
                } else if (settingsCallback != null) {
                    settingsCallback.call(this);
                }
            });
        },
        deleteManga: function () {
            this.showModalDialog("Attention", "Are you sure?", "Deleting a saved VisAlyzer is irreversible", "delete_manga");
        },
        takeAction: function () {
            if (this.modal_yes_no_action == "delete_manga") {
                axios.get('/api/visualize/delete_manga', {
                    params: {
                        inctrak_id: document.getElementById('inctrak_id').value,
                        vis_id: this.vis_id
                    }
                })
                .then(response => {
                    if (!response.data) {
                        this.showModalDialog("Error", "Vis: no data returned", "");
                    } else if (!response.data.Success) {
                        this.showModalDialog("Error", "Vis: could not delete.", "");
                    } else {
                        window.location = '/index.html';
                    }
                })
                .catch(error => {
                    this.showModalDialog("Unknown Error", "Vis: unknown error deleting.", error);
                });
            }
        },
        exportSelectedAsCSV: function () {
            this.statusStart = new Date();
            this.statusCheckCount = 0;
            this.showModalStatusDialog("NOP", { "Download": { "Started": this.statusStart, "Elapsed": "00:00:00", "Desc": "Download CSV data", "Status": "Starting" } });
            axios.get('/api/visualize/export_csv', {
                responseType: 'blob',
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value,
                    vis_id: this.vis_id
                },
                onDownloadProgress: (progressEvent) => {
                    if ((this.statusCheckCount % 10) == 0) {
                        this.showModalStatusDialog("NOP", { "Download": { "Started": this.statusStart, "Elapsed": elapsed(this.statusStart), "Desc": "Downloaded " + progressEvent.loaded + " CSV data", "Status": "Running" } });
                    }
                    this.statusCheckCount = this.statusCheckCount + 1;
                    //let percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total); // you can use this to show user percentage of file downloaded
                }
            })
            .then(response => {
                this.showModalStatus = false;
                const url = window.URL.createObjectURL(new Blob([response.data]));
                const link = document.createElement('a');
                link.href = url;
                link.setAttribute('download', 'vis_manga.csv'); //or any other extension
                document.body.appendChild(link);
                link.click();
            })
            .catch(error => {
                this.showModalStatus = false;
                this.showModalDialog("Unknown Error", "Vis: unknown error exporting.", error);
            });
        },
        handleScroll: function (event) {
            this.setVisSize();
        },
        bucketDefintion: function () {
            if (this.selectedColumn) {
                if (this.selectedColumn.bucketized) {
                    var desc;
                    if (this.selectedColumn.dataType == "Numeric") {
                        var numericType = "";
                        if (this.selectedColumn.curBucketSize == 0) {
                            numericType = " range buckets";
                        } else {
                            numericType = " average/outlier buckets";
                        }
                        desc = " - " + this.selectedColumn.curBucketMod.toLocaleString() + numericType;
                    } else if (this.selectedColumn.dataType == "Date" || this.selectedColumn.dataType == "Text") {
                        var buckets;
                        if (this.selectedColumn.dataType == "Date") {
                            buckets = dateBuckets;
                        } else if (this.selectedColumn.dataType == "Text") {
                            buckets = textBuckets
                        }
                        for (var i = 0; i < buckets.length; i++) {
                            if (buckets[i].value == this.selectedColumn.curBucketSize) {
                                desc = " - " + buckets[i].text + " ";
                                break;
                            }
                        }
                    }

                    return this.selectedColumn.nodes.length + desc;
                } else {
                    return "Native";
                }
            }
            return "";
        },
        nodeDefintion: function () {
            if (this.selectedNode) {
                if (this.selectedNode.duplicates > 1) {
                    return this.selectedNode.value + " (" + this.selectedNode.duplicates + " different case)";
                } else {
                    return this.selectedNode.value;
                }
            }
            return "";
        },
        curBucketized: function () {
            if (this.selectedColumn) {
                return this.selectedColumn.bucketized;
            }
            return false;
        },
        curBucketSize: function () {
            if (this.selectedColumn) {
                return this.selectedColumn.curBucketSize;
            }
            return null;
        },
        curBucketMod: function () {
            if (this.selectedColumn) {
                return this.selectedColumn.curBucketMod;
            }
            return null;
        },
        onSubmitDoNothing: function () {

        },
        setBuckets: function (bucketized, bucketsize, bucketmod) {
            if (this.selectedColumn.bucketized != bucketized || this.selectedColumn.curBucketSize != bucketsize || this.selectedColumn.curBucketMod != bucketmod) {
                var colSpec = this.selectedColumn.name + ":" + this.selectedColumn.index;
                if (this.selection_expression && this.selection_expression.indexOf(colSpec) != -1) {
                    this.showModalDialog("Attention", "Column is filtered, please remove filter to rebucketize", "")
                } else {
                    axios.get('/api/visualize/bucketize', {
                        params: {
                            inctrak_id: document.getElementById('inctrak_id').value,
                            vis_id: this.vis_id,
                            column_name: this.selectedColumn.name,
                            column_index: this.selectedColumn.index,
                            bucketized: bucketized,
                            bucket_size: bucketsize,
                            bucket_mod: bucketmod,
                        }
                    })
                    .then(response => {
                        if (!response.data) {
                            this.data = null;
                            this.showModalDialog("Error", "Vis: no data for bucketization.", "");
                        } else {
                            this.showModalStatusDialog(response.data.PickupKey, response.data.StatusData);
                            //this.data = response.data;
                            //this.drawNodes();
                        }
                    })
                    .catch(error => {
                        this.showModalDialog("Unknown Error", "Vis: unknown error bucketing.", error);
                    });
                }
            }
        },
        displayColumnSettings: function () {
            this.colMenuDisplay = "none";
            this.showModalColumn = true;
        },
        getDetailedColStats: function () {
            this.colMenuDisplay = "none";
            axios.get('/api/visualize/detailed_col_stats', {
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value,
                    vis_id: this.vis_id,
                    column_name: this.selectedColumn.name,
                    column_index: this.selectedColumn.index
                }
            })
            .then(response => {
                if (response.data && response.data.Success) {
                    this.colStatistics = response.data.ColStats;
                    this.showModalStatsDialog();
                } else {
                    this.showModalDialog("Unknown Error", "Vis: failure getting column stats.");
                }
            })
            .catch(error => {
                this.showModalDialog("Unknown Error", "Vis: unknown error column stats.", error);
            });
        },
        getDetailedColReport: function () {
            this.colMenuDisplay = "none";
            var colReport = {
                report_name: "Column " + this.selectedColumn.name + " Report",
                columns: ["Value", "Percent of Total", "Sel Pct of Total", "Pct of Sel", "Rows", "Selected Rows"],
                data: []
            };
            for (var i = 0; i < this.selectedColumn.nodes.length; i++) {
                var node = this.selectedColumn.nodes[i];
                colReport.data.push([node.value, node.rawCol.TotalPct, node.rawCol.SelectAllPct, node.rawCol.SelectRelPct, node.rawCol.TotalValues, node.rawCol.SelectedValues]);
            }

            this.curreportdata = colReport;
            this.showModalReport = true;
        },
        showMeasures: function () {
            this.cellMenuDisplay = "none";
            this.showModalMeasures = true;
        },
        showNodeRows: function () {
            this.cellMenuDisplay = "none";
            if (this.selectedNode == null) {
                this.showModalDialog("Error", "Vis: cannot determine node, please try again.");
            } else if (this.selectedNode.rawCol.SelectedValues > 10000) {
                this.showModalDialog("Error", "Vis: too many rows for quick view (over 10000) please export.");
            } else {
                axios.get('/api/visualize/get_node_rows', {
                    params: {
                        inctrak_id: document.getElementById('inctrak_id').value,
                        vis_id: this.vis_id,
                        col_index: this.selectedColumn.index,
                        selected_node: this.selectedNode.value + "@" + this.selectedColumn.name + ":" + this.selectedColumn.index,
                        filtered: true
                    }
                })
                .then(response => {
                    if (response.data && response.data.Success && response.data.ReportData) {
                        var colReport = {
                            report_name: "Column " + this.selectedColumn.name + " Report",
                            columns: response.data.ReportData.columns,
                            data: response.data.ReportData.data
                        };

                        this.curreportdata = colReport;
                        this.showModalReport = true;
                    } else {
                        visualizer.showModalDialog("Unknown Error", "Vis: failure getting column rows.");
                    }
                })
                .catch(error => {
                    visualizer.showModalDialog("Unknown Error", "Vis: unknown error column rows.", error);
                });
            }
        },
        showDuplicateEntries: function() {
            this.cellMenuDisplay = "none";
            if (this.selectedNode == null) {
                this.showModalDialog("Error", "Vis: cannot determine node, please try again.");
            } else {
                axios.get('/api/visualize/get_duplicate_entries', {
                    params: {
                        inctrak_id: document.getElementById('inctrak_id').value,
                        vis_id: this.vis_id,
                        col_index: this.selectedColumn.index,
                        selected_node: this.selectedNode.value + "@" + this.selectedColumn.name + ":" + this.selectedColumn.index,
                        filtered: true
                    }
                })
                .then(response => {
                    if (response.data && response.data.Success && response.data.DuplicateEntries) {
                        this.showModalDialog("Duplicate Entries for " + this.selectedColumn.name + ": " + this.selectedNode.value, null, null, null, response.data.DuplicateEntries);
                    } else {
                        visualizer.showModalDialog("Unknown Error", "Vis: failure getting duplicates cell.");
                    }
                })
                .catch(error => {
                    visualizer.showModalDialog("Unknown Error", "Vis: unknown error duplicates cell.", error);
                });
            }
        },
        showDependencyDiagram: function() {
            this.cellMenuDisplay = "none";
            if (this.selectedNode == null) {
                this.showModalDialog("Error", "Vis: cannot determine node, please try again.");
            } else {
                axios.get('/api/visualize/get_dependency_diagram', {
                    params: {
                        inctrak_id: document.getElementById('inctrak_id').value,
                        vis_id: this.vis_id,
                        col_index: this.selectedColumn.index,
                        selected_node: this.selectedNode.value + "@" + this.selectedColumn.name + ":" + this.selectedColumn.index,
                        filtered: true
                        }
                    })
                    .then(response => {
                        if (response.data && response.data.Success && response.data.DependencyDiagram) {
                            this.curdepdata = response.data.DependencyDiagram;
                            this.showModalDep = true;
                        } else {
                            visualizer.showModalDialog("Unknown Error", "Vis: failure getting dependency diagram cell.");
                        }
                    })
                    .catch(error => {
                        visualizer.showModalDialog("Unknown Error", "Vis: unknown error dependency diagram cell.", error);
                    });
            }
        },
        isTextType: function () {
            return this.selectedColumn && this.selectedColumn.dataType == "Text";
        },
        showChart: function () {
            this.showModalChart = true;
        },
        showFilter: function () {
            this.showModalExpression = true;
        },
        goHome: function () {
            window.location = "/index.html";
        },
    }
})
