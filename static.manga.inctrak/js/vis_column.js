var vueColumnDefinition = {
    template: null,
    props: {
        show: Boolean,
        data: Object,
        curbucketized: Boolean,
        curbucketsize: Number,
        curbucketmod: Number,
    },
    data: function () {
        return {
            bucketized: this.curbucketized,
            bucketsize: this.curbucketsize,
            bucketmod: this.curbucketmod,
        }
    },
    watch: {
        curbucketized: function (bucketized) {
            this.bucketized = bucketized;
        },
        curbucketsize: function (bucketsize) {
            this.bucketsize = bucketsize;
        },
        curbucketmod: function (bucketmod) {
            this.bucketmod = bucketmod;
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
        },
        numeric_options: function () {
            var ret = []
            for (var i = 0; i < numericBuckets.length; i++) {
                ret.push(numericBuckets[i]);
            }
            return ret;
        },
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
            return this.data.minBucketSize;
        },
        minBucketMod: function () {
            if (this.data) {
                if (this.bucketsize == 1) {
                    return 2;
                } else {
                    return this.data.minBucketMod;
                }
            }
        },
        maxBucketMod: function () {
            if (this.data) {
                if (this.bucketsize == 1) {
                    return 100;
                } else {
                    return Number.MAX_SAFE_INTEGER;
                }
            }
        },
        allowedBuckets: function () {
            return this.data && this.data.allowedBuckets;
        },
        invalidBucketSize: function () {
            if (this.data) {
                if (this.data.dataType == "Text") {
                    return this.data && !this.data.allowedBuckets.includes(this.bucketsize);
                } else if (this.data.dataType == "Date") {
                    return this.data && this.bucketsize < this.minBucketSize();
                } else if (this.data.dataType == "Numeric") {
                    return this.data && this.bucketsize < this.minBucketSize() && this.bucketmod < this.minBucketMod() && this.bucketmod > this.maxBucketMod();
                }
            }
        },
        onSubmitDoNothing: function () {

        },
        savePost: function () {
            // Some save logic goes here...
            this.$emit('close');
            this.$emit('ok', this.bucketized, this.bucketsize, this.bucketmod);
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

