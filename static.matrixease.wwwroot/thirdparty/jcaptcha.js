(function (root, factory) {
    if (root === undefined && window !== undefined) root = window;
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module unless amdModuleId is set
        define([], function () {
            return (root['jCaptcha'] = factory());
        });
/***********************Joe removed as screw up Electron Distribution************************
    } else if (typeof module === 'object' && module.exports) {
        // Node. Does not work with strict CommonJS, but
        // only CommonJS-like environments that support module.exports,
        // like Node.
        module.exports = factory();
************************Joe removed as screw up Electron Distribution************************/
    } else {
        root['jCaptcha'] = factory();
    }
}(this, function () {

    "use strict";

    {
        var generateRandomNum = function generateRandomNum(jcap) {
            axios.get('/captcha', {
                params: {
                    matrixease_id: document.getElementById('matrixease_id').value
                }
            })
                .then(response => {
                    num1 = response.data.num1;
                    num2 = response.data.num2;
                    sumNum = num1 + num2;
                    setCaptcha.call(jcap, jcap.$el, jcap.options);
                })
                .catch(error => {
                });
        };

        var setCaptcha = function setCaptcha($el, options, shouldReset) {
            if (!shouldReset) {
                $el.insertAdjacentHTML('beforebegin', "<canvas class=\"".concat(options.canvasClass, "\"\n                    width=\"").concat(options.canvasStyle.width, "\" height=\"").concat(options.canvasStyle.height, "\">\n                </canvas>\n            "));
                this.$captchaEl = document.querySelector(".".concat(options.canvasClass));
                this.$captchaTextContext = this.$captchaEl.getContext('2d');
                this.$captchaTextContext = Object.assign(this.$captchaTextContext, options.canvasStyle);
            }

            this.$captchaTextContext.clearRect(0, 0, options.canvasStyle.width, options.canvasStyle.height);
            this.$captchaTextContext.fillText("".concat(num1, " + ").concat(num2, " ").concat(options.requiredValue), 0, 0);
        };

        var jCaptcha = function jCaptcha() {
            var options = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
            this.options = Object.assign({}, {
                el: '.jCaptcha',
                canvasClass: 'jCaptchaCanvas',
                requiredValue: '*',
                resetOnError: true,
                focusOnError: true,
                clearOnSubmit: false,
                callback: null,
                canvasStyle: {}
            }, options);

            this._init();
        };

        var sumNum, num1, num2;
        var numberOfTries = 0;
        ;
        jCaptcha.prototype = {
            _init: function _init() {
                this.$el = document.querySelector(this.options.el);
                generateRandomNum(this);
            },
            validate: function validate() {
                numberOfTries++;
                this.callbackReceived = this.callbackReceived || typeof this.options.callback == 'function';

                if (this.$el.value != sumNum) {
                    this.callbackReceived && this.options.callback('error', this.$el, numberOfTries);
                    this.options.resetOnError === true && this.reset();
                    this.options.focusOnError === true && this.$el.focus();
                    this.options.clearOnSubmit === true && (this.$el.value = '');
                } else {
                    this.callbackReceived && this.options.callback('success', this.$el, numberOfTries, [num1, num2, sumNum].join(","));
                    this.options.clearOnSubmit === true && (this.$el.value = '');
                }
            },
            reset: function reset() {
                generateRandomNum(this);
            }
        };
    }

    return jCaptcha;

}));
