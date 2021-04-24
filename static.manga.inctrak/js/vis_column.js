var vueColumnDefinition = {
    template: null,
    props: {
        show: Boolean,
        data: Object,
        curbucketized: Boolean,
        curbucketsize: Number
    },
    data: function () {
        return {
            bucketized: this.curbucketized,
            bucketsize: this.curbucketsize
        }
    },
    watch: {
        curbucketized: function (bucketized) {
            this.bucketized = bucketized;
        },
        curbucketsize: function (bucketsize) {
            this.bucketsize = bucketsize;
        }
    },
    computed: {
        colAttributes1: function () {
            return colAttributes1(this.data, true);
        },
        colAttributes2: function () {
            return colAttributes2(this.data, true);
        },
        colAttributes3: function () {
            return colAttributes3(this.data, true);
        },
        colAttributes4: function () {
            return colAttributes4(this.data, true);
        },
        colAttributes5: function () {
            return colAttributes5(this.data, true);
        },
        colAttributes6: function () {
            return colAttributes6(this.data, true);
        },
        isOnlyBucketable: function () {
            return this.data && this.data.onlyBuckets;
        },
        date_options: function () {
            var ret = []
            for (var i = 0; i < dateBuckets.length; i++) {
                if (dateBuckets[i].value >= this.minBucketSize()) {
                    ret.push(dateBuckets[i]);
                }
            }
            return ret;
        },
        text_options: function () {
            var ret = []
            for (var i = 0; i < textBuckets.length; i++) {
                if (this.allowedBuckets().includes(textBuckets[i].value)) {
                    ret.push(textBuckets[i]);
                }
            }
            return ret;
        }
    },
    methods: {
        numericType: function () {
            return this.data && this.data.dataType == "Numeric";
        },
        dateType: function () {
            return this.data && this.data.dataType == "Date";
        },
        textType: function () {
            return this.data && this.data.dataType == "Text";
        },
        minBucketSize: function () {
            return this.data && this.data.minBucketSize;
        },
        allowedBuckets: function () {
            return this.data && this.data.allowedBuckets;
        },
        invalidBucketSize: function () {
            if (this.data && this.data.dataType == "Text") {
                return this.data && !this.data.allowedBuckets.includes(this.bucketsize);
            } else {
                return this.data && this.bucketsize < this.minBucketSize();
            }
        },
        onSubmitDoNothing: function () {

        },
        savePost: function () {
            // Some save logic goes here...
            this.$emit('close');
            this.$emit('ok', this.bucketized, this.bucketsize);
        }
    }
};

function vueColumnResolver(resolve, reject) {
    axios.get('/comp/modal_column.html')
        .then(response => {
            vueColumnDefinition.template = response.data;
            resolve(vueColumnDefinition);
        });
}

Vue.component('modal_column', vueColumnResolver);

