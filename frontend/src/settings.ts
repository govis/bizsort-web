// @ts-nocheck
﻿export const Company = {
    id: 1
};

export const Category = {
    nameLength: 50
};

export const Community = {
    blog: 4,
    forum: {
        MaxLength: 30
    }
};

export const Guid = {
    empty: "00000000-0000-0000-0000-000000000000"
};

export const Image = (function () {
    return {
        thumbnail: {
            width: 130,
            height: 0
        },
        wideThumbnail: {
            width: 180,
            height: 120
        },
        xtraSmall: {
            width: 150,
            height: 0
        },
        small: {
            width: 240,
            height: 0
        },
        mediumSmall: {
            width: 320,
            height: 0
        },
        medium: {
            width: 640,
            height: 0
        },
        jpegQuality: 80,
        sizeThreshold: 85
    }
})();

export const Location = {
    country: { id: 1, name: "Canada", code: "ca" }, //ISO 3166-1 country code for GeocoderComponentRestrictions
    //Country: { Id: 15, Name: "United States", Code: "us" }, //ISO 3166-1 country code for GeocoderComponentRestrictions
    address1Threshold: 10,

    data: {
        "Canada": {
            "Phone": {
                userEntry: {
                    "mask": "000-000-0000"
                },
                ValueFormat: {
                    Steps: [{
                        "Type": "RegexReplace",
                        "Match": "[^0-9]",
                        "Replace": ""
                    }, {
                        "Type": "Format18xx"
                    }, {
                        "Type": "RegexReplace",
                        "Match": "(\\d{3})(\\d{3})(\\d{4})",
                        "Replace": "$1-$2-$3"
                    }]
                }
            },
            "PostalCode": {
                userEntry: {
                    "mask": "L0L 0L0"
                },
                ValueFormat: {
                    Steps: [{
                        "Type": "UpperCase"
                    }, {
                        "Type": "RegexReplace",
                        "Match": "([A-Z]\\d[A-Z])( )?(\\d[A-Z]\\d)",
                        "Replace": "$1 $3"
                    }]
                }
            }
        },
        "United States": {
            "Phone": {
                userEntry: {
                    "mask": "000-000-0000"
                },
                ValueFormat: {
                    Steps: [{
                        "Type": "RegexReplace",
                        "Match": "[^0-9]",
                        "Replace": ""
                    }, {
                        "Type": "Drop1in18xx"
                    }, {
                        "Type": "RegexReplace",
                        "Match": "(\\d{3})(\\d{3})(\\d{4})",
                        "Replace": "$1-$2-$3"
                    }]
                }
            },
            "PostalCode": {
                userEntry: {
                    "mask": "00000"
                }
            }
        },
        "Australia": {
            "Phone": {
                userEntry: {
                    "mask": "000000000"
                }
            },
            "PostalCode": {
                userEntry: {
                    "mask": "0000"
                }
            }
        },
        "New Zealand": {
            "Phone": {
                userEntry: {
                    "mask": "000000009",
                    "prompt": "Right-pad with a space if necessary"
                }
            },
            "PostalCode": {
                userEntry: {
                    "mask": "0000"
                }
            }
        },
        "United Kingdom": {
            "Phone": {
                userEntry: {
                    "mask": "0000000009",
                    "prompt": "Right-pad with a space if necessary"
                }
            },
            "PostalCode": {
                userEntry: {
                    "mask": "aaAA 0LL",
                    "prompt": "Left-pad first part with 1 or 2 spaces if necessary",
                    "PartSeparator": " ",
                    "Parts": [{
                        "PadToLength": 4
                    }]
                }
            }
        }
    },

    getSettings: function (location) {
        var locationSettings = {};
        if (location && this.data)
            this.populateSettings(location, this.data, locationSettings);
        return locationSettings;
    },

    populateSettings: (location, locationSettings, effectiveSettings) => {
        var locations = [];
        while (location && location.id > 0) {
            locations.splice(0, 0, location.name);
            location = location.parent;
        }
        var locationName;
        for (var i = 0, l = locations.length; i < l; i++) {
            locationName = locations[i];
            locationSettings = locationSettings[locationName];

            if (locationSettings)
                Location.populate.call(effectiveSettings, locationSettings);
            else
                break;
        }

        return effectiveSettings;
    },

    populate: function (settings) {
        if (!this.phone)
            this.phone = {};
        Location._populate.call(this.phone, settings.phone);
        if (!this.postalCode)
            this.postalCode = {};
        Location._populate.call(this.postalCode, settings.postalCode);
    },

    _populate: function (settings) {
        if (settings) {
            if (settings.userEntry) {
                if (!this.userEntry)
                    this.userEntry = {};
                if (settings.userEntry.mask) {
                    this.userEntry.mask = settings.userEntry.mask;
                }
                if (settings.userEntry.prompt) {
                    this.userEntry.prompt = settings.userEntry.prompt;
                }
            }
        }
    }
}

export const Personal = {
    list: {
        maxLength: 50
    },

    message: {
        folder: {
            maxLength: 30
        }
    }
};

export const Product = {
    quota: {
        personal: {
            Total: 101,
            Active: 11,
            Pending: 2
        },

        company: {
            Total: 1001,
            Active: 101,
            Pending: 5
        }
    }
};

export const Session = {
    storageItemName: "bizsrtSession",
    httpHeader: {
        token: "X-BizSrt-Session",
        key: "Authorize"
    },
    autoSignin: {
        cookieName: "BizSrt.User.Token",
        expireAfter: 10
    }
};

export const Service = {
    origin: "http://localhost:8000", //http://localhost:8000  //http://bizsrt.svc
    httpHeader: {
        fault: "X-BizSrt-Fault"
    },
    facebook: {
        appId: "944137705659654"
    },
    google: {
        clientId: "549148705671-ja1hg0pm9bh4nqntldrk66rnpfl51cmh.apps.googleusercontent.com",
        apiKey: "AIzaSyDdI1DHuEPefTcLwmJqZA8aozrsQDOOaAw",
        siteKey: "6LcAKtYUAAAAAM4pMPEBo8KUxpdbVCoNPncLXILe"
    }
};

export const WebSite = {
    origin: {
        host: "localhost:50000",
        serverPath: "",
        absoluteUri: ""
    },
    homePage: "/directory",
    navToken: {
        Placement: 1, //NavigationTokenPlacement.QueryString
        qualifier: "t", //"#!"
    },
    mobileUrl: "http://localhost:50003",
    formsUrl: "http://localhost:50002/",
    otherWeb: "ca"
};
