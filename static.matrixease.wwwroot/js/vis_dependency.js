var vueDependencyDefinition = {
    template: null,
    props: {
        show: Boolean,
        curdepdata: Object,
    },
    data: function () {
        return {
            colData: null,
        }
    },
    watch: {
        curdepdata: function (curdepdata) {
            this.curdepdata = curdepdata;
        },
    },
    methods: {
        onSubmitDoNothing: function () {
        },
        onChange() {
            initChords();
            updateChords(this.colData.keys, this.colData.matrix);
        },
        openColDependencyWindow: function () {
            var visDependencies = customWindowOpen("/mxes_dependencies.html", "_blank",
                {
                    depData: {
                        curdepdata: this.curdepdata
                    }
                }
            );
        },
        cancel: function () {
            // Some save logic goes here...
            this.$emit('close');
        }
    }
};

function vueDependencyResolver(resolve, reject) {
    axios.get('/comp/modal_dependency.html')
        .then(response => {
            vueDependencyDefinition.template = response.data;
            resolve(vueDependencyDefinition);
        });
}

Vue.component('modal_dependency', vueDependencyResolver);

