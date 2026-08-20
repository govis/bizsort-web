import { API_BASE } from './api.js';

export function getLogoUrl(entity: number, imageId: number | undefined, width: number = 200, height?: number): string {
    if (!imageId) return '/images/bizsort-logo.svg';
    
    let url = `${API_BASE}/api/image/get?entity=${entity}&id=${imageId}&width=${width}`;
    if (height !== undefined) {
        url += `&height=${height}`;
    }
    return url;
}

export function analyzeImage(img: HTMLImageElement, border: number = 5): { background: { r: number, g: number, b: number }, foreground?: { r: number, g: number, b: number } } {
    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");
    
    if (!ctx) {
        return { background: { r: 255, g: 255, b: 255 } };
    }

    const w = img.naturalWidth;
    const h = img.naturalHeight;

    canvas.width = w;
    canvas.height = h;

    ctx.drawImage(img, 0, 0);

    // get borders, avoid overlaps
    const top = ctx.getImageData(0, 0, w, border).data;
    const left = ctx.getImageData(0, border, border, h - border * 2).data;
    const right = ctx.getImageData(w - border, border, border, h - border * 2).data;
    const bottom = ctx.getImageData(0, h - border, w, border).data;

    let r = 0, g = 0, b = 0, t = 0, cnt = 0;

    const addPixels = function (data: Uint8ClampedArray) {
        let i = 0, len = data.length;
        while (i < len) {
            r += data[i++];
            g += data[i++];
            b += data[i++];
            if (!data[i++]) {
                t++;
            }
            cnt++;
        }
    };

    addPixels(top);
    addPixels(left);
    addPixels(right);
    addPixels(bottom);

    // calc average
    if ((t / cnt) < 0.25) {
        r = Math.floor(r / cnt + 0.5);
        g = Math.floor(g / cnt + 0.5);
        b = Math.floor(b / cnt + 0.5);
        t = Math.floor(t / cnt * 100);

        return {
            background: {
                r: r,
                g: g,
                b: b
            }
        }
    }

    const middle = ctx.getImageData(border, border, w - border * 2, h - border * 2).data;
    addPixels(middle);

    r = Math.floor(r / cnt + 0.5);
    g = Math.floor(g / cnt + 0.5);
    b = Math.floor(b / cnt + 0.5);

    const foreground = {
        r: r,
        g: g,
        b: b
    }

    const brightness = 1;
    r = Math.floor((255 - r) * brightness);
    g = Math.floor((255 - g) * brightness);
    b = Math.floor((255 - b) * brightness);

    return {
        foreground: foreground,
        background: {
            r: r,
            g: g,
            b: b
        }
    };
}

// ---------------------------------------------------------
// Authoritative Legacy Image Sizing Configurations
// ---------------------------------------------------------

export enum ImageEntity {
    Company = 1,
    Offering = 2, // Legacy Product
    Service = 3,
    Project = 4,
    Job = 5,
    Community = 6,
    CommunityArticle = 7,
    Person = 8,
    Organization = 9,
    Marketplace = 10,
    Promotion = 11,
    Website = 12
}

export const ImageSizes = {
    Company: {
        Card: { width: 240, height: 120 }, // 2 * CARD_IMAGE_HEIGHT(120)
        List: { width: 200, height: 100 },
        ViewHead: { width: 290, height: 145 }
    },
    Offering: {
        Card: { width: 240, height: 120 },
        List: { width: 120, height: 120 },
        ViewHead: { width: 300, height: 300 } // Legacy VIEW_IMAGE_WIDTH/HEIGHT
    },
    Community: {
        Card: { width: 240, height: 120 },
        List: { width: 240, height: 120 }
    },
    Person: {
        ViewHead: { width: 290, height: 145 }
    },
    News: {
        Icon: { width: 90, height: 90 }
    },
    Article: {
        Icon: { width: 90, height: 90 }
    }
};
