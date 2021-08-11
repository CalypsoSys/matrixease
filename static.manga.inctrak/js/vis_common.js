var numericBuckets = [
    { "value": 0, "text": "Ranges" },
    { "value": 1, "text": "Max averge/outlier buckets" },
];

function numericAverage(attr) {
    return "Average: " + attr.Average.toFixed(6);
}

function numericSmallest(attr) {
    return "Smallest: " + attr.Smallest;
}

function numericLargest(attr) {
    return "Largest: " + attr.Largest;
}

function numericStandardDeviation(attr) {
    return "Standard Deviation: " + attr.StandardDeviation.toFixed(6);
}

function numericCoefficientOfVariation(attr) {
    return "Coefficient of Variation: " + attr.CoefficientOfVariation.toFixed(6);
}

var dateBuckets = [
    { "value": 2, "text": "Milli-Second buckets" },
    { "value": 3, "text": "1 Second buckets" },
    { "value": 4, "text": "1 Minute buckets" },
    { "value": 5, "text": "1 Hour buckets" },
    { "value": 6, "text": "1 Day buckets" },
    { "value": 7, "text": "1 Week buckets" },
    { "value": 8, "text": "1 Month buckets" },
    { "value": 9, "text": "1 Year buckets" },
    { "value": 10, "text": "1 Decade buckets" },
    { "value": 11, "text": "1 Century buckets" },
    { "value": 12, "text": "Months of the Year" },
    { "value": 13, "text": "Days of the Week" },
    { "value": 14, "text": "Hours of the Day" },
    { "value": 15, "text": "Minutes of the Hour" },
    { "value": 16, "text": "Seconds of the Minute" }
];
function dateEarliest(attr) {
    return "Earliest: " + new Date(attr.Earliest).toLocaleDateString();
}

function dateLatest(attr) {
    return "Latest: " + new Date(attr.Latest).toLocaleDateString();
}

function dateAverage(attr) {
    return "Average: " + new Date(attr.Average).toLocaleDateString();
}

function dateStdDevDays(attr) {
    return "Standard Deviation Days: " + attr.StdDevDays;
}

function dateCoefVarDays(attr) {
    return "Coefficient of Variation Days: " + attr.CoefVarDays;
}

var textBuckets = [
    { "value": 0, "text": "Natural" },
    { "value": 1, "text": "One Letter Prefixes" },
    { "value": 2, "text": "Two Letter Prefixes" },
    { "value": 3, "text": "Three Letter Prefixes" },
    { "value": 4, "text": "Four Letters Prefixes" },
    { "value": 5, "text": "Word Patterns"},
    { "value": 6, "text": "Most Significant Terms"},
    { "value": 7, "text": "Third Url Path"},
    { "value": 8, "text": "Second Url Path"},
    { "value": 10, "text": "First Url Path"},
    { "value": 11, "text": "Url Domains"},
    { "value": 12, "text": "Url Schemes"},
    { "value": 13, "text": "Common Url Path Terms"},
    { "value": 14, "text": "Text Length"}
];
function textAvgTextLength(attr) {
    return "Average Text Length: " + attr.AvgTextLength.toFixed(2);
}

function textShortestLen(attr) {
    return "Shortest Text Length: " + attr.ShortestText;
}

function textLongestLen(attr) {
    return "Longest Text Length: " + attr.LongestText;
}

function textAvgNumTerms(attr) {
    return "Average Number of Terms: " + attr.AvgTerms.toFixed(2);
}

function textUrlSchemes(attr) {
    return "URL Schemes: " + attr.UrlScheme;
}

function textUrlDomains(attr) {
    return "URL Domains: " + attr.UrlDomain;
}

function textPrefixes(attr) {
    var prefixes = 0;
    var kind = "";
    if (attr.Prefix1) {
        prefixes = attr.Prefix1;
        kind = "1";
    }
    if (attr.Prefix2 && attr.Prefix2 >= prefixes) {
        prefixes = attr.Prefix2;
        kind = "2";
    }
    if (attr.Prefix3 && attr.Prefix3 >= prefixes) {
        prefixes = attr.Prefix3;
        kind = "3";
    }
    if (attr.Prefix4 && attr.Prefix4 >= prefixes) {
        prefixes = attr.Prefix4;
        kind = "4";
    }
    if (prefixes > 0) {
        return kind + " Letter Prefixes: " + prefixes;
    }
    return "";
}

function textTermsStat(attr) {
    if (attr.TermPatterns) {
        return "Term Patterns: " + attr.TermPatterns;
    } else if (attr.TermWeigths) {
        return "Considered Term Weights: " + attr.TermWeigths;
    } else if (attr.DocTermCounts) {
        return "Document Terms: " + attr.DocTermCounts;
    }
    return "";
}

function colAttributes1(selectedColumn, popup) {
    if (selectedColumn) {
        switch (selectedColumn.dataType) {
            case "Numeric":
                return numericAverage(selectedColumn.attr);
            case "Text":
                return textAvgTextLength(selectedColumn.attr);
            case "Date":
                return dateEarliest(selectedColumn.attr);
        }
    }
    return "";
}

function colAttributes2(selectedColumn, popup) {
    if (selectedColumn) {
        switch (selectedColumn.dataType) {
            case "Numeric":
                if (popup == false)
                    return numericSmallest(selectedColumn.attr);
                break;
            case "Date":
                return dateLatest(selectedColumn.attr);
            case "Text":
                if (selectedColumn.attr.IsUrl)
                    return "Column is a URL";
                else
                    return textAvgNumTerms(selectedColumn.attr);
        }
    }
    return "";
}

function colAttributes3(selectedColumn, popup) {
    if (selectedColumn) {
        switch (selectedColumn.dataType) {
            case "Numeric":
                if (popup) {
                    return numericSmallest(selectedColumn.attr);
                } else {
                    return numericLargest(selectedColumn.attr);
                }
            case "Date":
                return dateAverage(selectedColumn.attr);
            case "Text":
                return textShortestLen(selectedColumn.attr);
        }
    }
    return "";
}

function colAttributes4(selectedColumn, popup) {
    if (selectedColumn) {
        switch (selectedColumn.dataType) {
            case "Numeric":
                if (popup) {
                    return numericLargest(selectedColumn.attr);
                } else {
                    return numericStandardDeviation(selectedColumn.attr);
                }
            case "Text":
                return textLongestLen(selectedColumn.attr);
        }
    }
    return "";
}

function colAttributes5(selectedColumn, popup) {
    if (selectedColumn) {
        switch (selectedColumn.dataType) {
            case "Numeric":
                return numericStandardDeviation(selectedColumn.attr);
            case "Date":
                return dateStdDevDays(selectedColumn.attr);
            case "Text":
                if (selectedColumn.attr.IsUrl)
                    return textUrlSchemes(selectedColumn.attr);
                else
                    return textPrefixes(selectedColumn.attr);
        }
    }
    return "";
}

function colAttributes6(selectedColumn, popup) {
    if (selectedColumn) {
        switch (selectedColumn.dataType) {
            case "Numeric":
                return numericCoefficientOfVariation(selectedColumn.attr);
            case "Date":
                return dateCoefVarDays(selectedColumn.attr);
            case "Text":
                if (selectedColumn.attr.IsUrl)
                    return textUrlDomains(selectedColumn.attr);
                else
                    return textTermsStat(selectedColumn.attr);
        }
    }
    return "";
}

function elapsed(startDate) {
    var seconds = (new Date() - startDate) / 1000;

    var hours = Math.floor(seconds / (60 * 60));
    seconds -= (hours * (60 * 60));
    if (hours < 10)
        hours = "0" + hours;

    var minutes = Math.floor(seconds / 60);
    seconds -= (minutes * 60);
    if (minutes < 10)
        minutes = "0" + minutes;

    seconds = Math.floor(seconds);
    if (seconds < 10)
        seconds = "0" + seconds;

    return hours + ":" + minutes + ":" + seconds;
}