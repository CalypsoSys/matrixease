var vueCookieDefinition =
{
    template: null,
    props: {
        show: Boolean,
    },
    methods: {
        onSubmitDoNothing: function () {

        },
        cookiesAccepted: function () {
            var expiryDate = new Date();
            expiryDate.setMonth(expiryDate.getMonth() + 6);
            this.$cookies.set("cookies-accepted-1", "acceptedxxx", expiryDate);
            this.$emit('close');
            this.$emit('accept_cookie', true);
        }
    }
};

function vueCookieResolver(resolve, reject) {
    axios.get('/comp/modal_cookie.html')
        .then(response => {
            vueCookieDefinition.template = response.data;
            resolve(vueCookieDefinition);
        });
}

Vue.component('modal_cookie', vueCookieResolver);
