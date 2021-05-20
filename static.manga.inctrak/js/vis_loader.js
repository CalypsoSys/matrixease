const maxNumberOfTries = 5;

var router = new VueRouter({
    mode: 'history',
    routes: []
});

var mangaAuth = new Vue({
    el: '#manga_auth',
    data: {
        incTrakKey: null,
        showModalCookie: false,
        showModalLogin: false,
        myMangas: null,
        my_tick: 0,
        sheet_type: "csv",
        file: '',
        uploadPercentage: 0,
        manga_name: "",
        header_row: 1,
        header_rows: 1,
        max_rows: 0,
        ignore_blank_rows: true,
        ignore_cols: "",
        sheet_id: "1VunkEZX3ajsXMerYXjahOevUd_p88vNrnI9QD2ByGvY",
        range: "108thHouse",
        showModalAlert: false,
        modal_title: "",
        modal_message: "",
        modal_secondary: "",
        modal_yes_no_action: "",
        showModalStatus: false,
        curstatuskey: "",
        curstatusdata: null
    },
    mounted: function () {
        document.onreadystatechange = () => {
            if (document.readyState == "complete") {
                this.incTrakKey = document.getElementById('inctrak_id').value;
                this.testAccess();
            }
        }
    },
    methods: {
        showModalDialog: function (title, message, secondary, yesNoAction) {
            this.modal_title = title;
            this.modal_message = message;
            this.modal_secondary = secondary;
            this.modal_yes_no_action = yesNoAction;
            this.showModalStatus = false;
            this.showModalAlert = true
        },
        showModalStatusDialog: function (curstatuskey, curstatusdata) {
            this.curstatuskey = curstatuskey;
            this.curstatusdata = curstatusdata;
            this.showModalStatus = true
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
                axios.get('/api/visualize/manga_status/', {
                    params: {
                        inctrak_id: document.getElementById('inctrak_id').value,
                        status_key: this.curstatuskey
                    }
                })
                    .then(response => {
                        if (response.data && response.data.Success) {
                            if (response.data.Complete) {
                                this.showModalStatus = false;
                                this.curstatusdata = null;
                                window.location = "/visualize.html?inctrak_id=" + encodeURIComponent(document.getElementById('inctrak_id').value) + "&vis_id=" + encodeURIComponent(this.curstatuskey);
                                this.curstatuskey = "";
                            } else {
                                this.curstatusdata = response.data.StatusData;
                            }
                        } else {
                            mangaAuth.showModalDialog("Error", "Vis: failure loading manga status.");
                        }
                    })
                    .catch(error => {
                        this.showModalStatus = false
                        mangaAuth.showModalDialog("Unknown Error", "Vis: unknown error checking manga status.", error);
                    });
            } else if (this.my_tick > 6) {
                this.my_tick = 0;
                this.showMyMangas();
            } else {
                this.my_tick = this.my_tick + 1;
            }
        },
        testAccess: function () {
            this.showModalCookie = false;
            this.showModalLogin = false;
            if (this.$cookies.get("cookies-accepted-1") != "acceptedxxx") {
                this.showModalCookie = true;
                this.showModalLogin = false;
            } else if (!this.$cookies.get("authenticated-accepted-1") ) {
                this.showModalLogin = true;
            } else {
                //this.showModalLogin = true;
                axios.get('/get_access/', {
                    params: {
                        inctrak_id: document.getElementById('inctrak_id').value,
                        access_token: this.$cookies.get("authenticated-accepted-1")
                    }
                })
                .then(response => {
                    var showLogin = true;
                    if (!response.data) {
                        this.data = null;
                        this.showModalDialog("Error", "Vis: no data returned", "");
                    } else if (!response.data.Success) {
                        //this.showModalDialog("Error", "Vis: authentication failed", "");
                        showLogin = true;
                    } else {
                        showLogin = (response.data.AccessToken != this.$cookies.get("authenticated-accepted-1"));
                    }
                    this.showModalLogin = showLogin;
                })
                .catch(error => {
                    this.showModalDialog("Unknown Error", "Vis: unknown error rendering.", error);
                });
            }

            if (this.showModalCookie == false && this.showModalLogin == false) {
                this.showMyMangas();
            }
        },
        acceptCookie: function () {
            this.testAccess();
        },
        authenticated: function () {
            this.testAccess();
        },
        showMyMangas: function () {
            axios.get('/my_mangas/', {
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value
                }
            })
            .then(response => {
                if (response.data && response.data.Success) {
                    this.myMangas = response.data.MyMangas;
                } else {
                    this.showModalDialog("Error", "Vis: no data for my manga catalog", "");
                }
            })
            .catch(error => {
                this.showModalDialog("Unknown Error", "Vis: unknown error retrieving manga catalog.", error);
            });
        },
        handleFileUpload() {
            this.file = this.$refs.file.files[0];
        },
        validateForm: function () {
            var error = ""
            if (!this.manga_name) {
                error += "Please enter Manga Name<br>";
            }
            if (this.header_row > this.header_rows) {
                error += "Header on Row cannot be greater then Header Rows<br>";
            }
            if (error) {
                this.showModalDialog("Error", "Vis: please check managa specification", error);
                return false;
            }

            return true;
        },
        submitUploadSheetForm() {
            if (!this.validateForm()) {
                return;
            }
            let formData = new FormData();
            formData.append('inctrak_id', document.getElementById('inctrak_id').value);
            formData.append('file', this.file);
            formData.append('manga_name', this.manga_name);
            formData.append('header_row', this.header_row);
            formData.append('header_rows', this.header_rows);
            formData.append('max_rows', this.max_rows);
            formData.append('ignore_blank_rows', this.ignore_blank_rows);
            formData.append('ignore_cols', this.ignore_cols);
            formData.append('sheet_type', this.sheet_type);

            axios.post('/upload_file',
                formData,
                {
                    headers: {
                        'Content-Type': 'multipart/form-data'
                    },
                    onUploadProgress: function (progressEvent) {
                        this.uploadPercentage = parseInt(Math.round((progressEvent.loaded / progressEvent.total) * 100));
                    }.bind(this)
                }
                ).then(response => {
                    if (response.data && response.data.Success) {
                        this.showModalStatusDialog(response.data.VisId, response.data.StatusData);
                    } else {
                        this.showModalDialog("Error", "Vis: failure uploading/processing Sheet.", response.data.Error);
                    }
                })
                .catch(error => {
                    this.showModalDialog("Unknown Error", "Vis: unknown error uploading/processing Sheet.", error);
                });
        },
        submitGoogleSheetForm: function () {
            if (!this.validateForm()) {
                return;
            }
            axios.get('/google/check_login/', {
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value
                }
            })
            .then(response => {
                if (response.data && response.data.Success) {
                    this.mangaGoogleSheet()
                } else {
                    this.showModalDialog("Attention", "You are not current authenticated against a Google account.", "Login Now?", "login_google");
                }
            })
            .catch(error => {
                this.showModalDialog("Unknown Error", "Vis: unknown error checking google creds.", error);
            });
        },
        takeAction: function () {
            if (this.modal_yes_no_action == "login_google") {
                this.$refs.google_login.submit()
            }
        },
        mangaGoogleSheet: function () {
            this.showModalStatusDialog("NOP", { "PreProcess": { "Started": new Date(), "Elapsed": "00:00:00", "Desc": "Getting data from Google Sheets", "Status": "Starting" } });
            axios.get('/google/sheet/', {
                params: {
                    inctrak_id: document.getElementById('inctrak_id').value,
                    manga_name: this.manga_name,
                    header_row: this.header_row,
                    header_rows: this.header_rows,
                    max_rows: this.max_rows,
                    ignore_blank_rows: this.ignore_blank_rows,
                    ignore_cols: this.ignore_cols,
                    sheet_id: this.sheet_id,
                    range: this.range
                }
            })
            .then(response => {
                if (response.data && response.data.Success) {
                    this.showModalStatusDialog(response.data.VisId, response.data.StatusData);
                } else {
                    this.showModalDialog("Error", "Vis: failure loading Google Sheet.", response.data.Error );
                }
            })
            .catch(error => {
                this.showModalDialog("Unknown Error", "Vis: unknown error checking google creds.", error);
            });
        }
    }
});
