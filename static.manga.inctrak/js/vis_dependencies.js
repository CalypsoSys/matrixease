var router = new VueRouter({
    mode: 'history',
    routes: []
});


var visDependencies = new Vue({
    router,
    el: '#vis_dependencies',
    data: {
        mangaName: "",
    },

    mounted: function () {
        document.onreadystatechange = () => {
            if (document.readyState == "complete") {
                renderWindowData(this.$route.query.id, this.renderChord);
            }
        }
    },
    methods: {
        renderChord: function (data) {
            initChords();
            updateChords(data.depData.keys, data.depData.matrix);
        }
    }
})
