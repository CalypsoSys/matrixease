﻿var vueAlertDefinition = {
    template: null,
    props: {
        show: Boolean,
        title: String,
        message: String,
        secondary: String,
        confirm: String
    },
    methods: {
        yesNo: function () {
            return this.confirm;
        },
        onSubmitDoNothing: function () {
        },
        sayYes: function () {
            // Some save logic goes here...
            this.$emit('close');
            this.$emit('yes', true);
        }
    }

};

function vueAlertResolver(resolve, reject) {
    axios.get('/comp/modal_alert.html')
    .then(response => {
        vueAlertDefinition.template = response.data;
        resolve(vueAlertDefinition);
    });
}

Vue.component('modal_alert', vueAlertResolver);
