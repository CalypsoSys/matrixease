var vueDependencyDefinition = {
    template: null,
    props: {
        show: Boolean,
        curdepdata: Object,
    },
    data: function () {
        return {
            myDependencies: null
        }
    },
    watch: {
        curdepdata: function (curdepdata) {
            this.curdepdata = curdepdata;
            initChords();
            updateChords(this.curdepdata.keys, this.curdepdata.matrix);
        },
    },
    methods: {
        onSubmitDoNothing: function () {
        },
        openColDependencyWindow: function () {
            var visDependencies = customWindowOpen("/vis_dependencies.html", "_blank",
                { depData:
                    {
                        chord_name: this.curdepdata.chord_name,
                        keys: this.curdepdata.keys,
                        matrix: this.curdepdata.matrix
                    }
            });
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

