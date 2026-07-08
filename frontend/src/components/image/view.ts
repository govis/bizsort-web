import { LitElement, html, css } from 'lit';
import { property, state } from 'lit/decorators.js';

import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';

export class ImageView extends LitElement {
    static get styles() {
        return css`
            :host {
                position: relative;
                display: inline-block;
                box-sizing: border-box;
                width: 100%;
                height: 100%;
                overflow: hidden;
            }

            img {
                width: 100%;
                height: 100%;
                object-fit: cover;
                display: block;
            }

            wa-button {
                display: none;
                position: absolute;
                top: 50%;
                transform: translateY(-50%);
                --wa-color-neutral-fill-normal: rgba(255,255,255,0.8);
            }

            :host(:hover) wa-button {
                display: inline-flex;
            }

            wa-button.prev {
                left: 10px;
            }

            wa-button.next {
                right: 10px;
            }

            .slider {
                position: absolute;
                bottom: 10px;
                left: 50%;
                transform: translateX(-50%);
                display: flex;
                gap: 8px;
                cursor: default;
            }

            .slider-knob {
                width: 12px;
                height: 12px;
                border-radius: 50%;
                border: 2px solid white;
                background-color: rgba(0,0,0,0.5);
                cursor: pointer;
            }

            .slider-knob.selected {
                background-color: white;
            }
        `;
    }

    protected render() {
        if (!this.images || this.images.length === 0) return html``;

        const imgRef = this._getImageUrl();
        
        return html`
            <img src="${imgRef}" alt="${this.alt || ''}"></img>
            ${this.images.length > 1 ? html`
                <wa-button variant="neutral" is-icon-button class="prev" ?disabled="${this._imageIndex <= 0}" @click=${() => this._handleAction(-1)}>
                    <wa-icon name="chevron-left" library="system"></wa-icon>
                </wa-button>
                <wa-button variant="neutral" is-icon-button class="next" ?disabled="${this._imageIndex >= this.images.length - 1}" @click=${() => this._handleAction(1)}>
                    <wa-icon name="chevron-right" library="system"></wa-icon>
                </wa-button>
                <div class="slider">
                    ${this.images.map((_, index) => html`
                        <div class="slider-knob ${index === this._imageIndex ? 'selected' : ''}" @click=${() => this._imageIndex = index}></div>
                    `)}
                </div>
            ` : ''}
        `;
    }

    @property({ type: Array })
    declare images: any[];

    @property({ type: String })
    declare alt?: string;

    @state()
    declare private _imageIndex: number;

    constructor() {
        super();
        this.images = [];
        this._imageIndex = 0;
    }

    private _getImageUrl(): string {
        const img = this.images[this._imageIndex];
        if (!img) return '';
        const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
        return \`\${backendUrl}/api/image/get?entity=\${img.entity}&id=\${img.id || img.imageId}&width=800&height=600\`;
    }

    private _handleAction(direction: number) {
        let newIndex = this._imageIndex + direction;
        if (newIndex < 0) newIndex = 0;
        if (newIndex >= this.images.length) newIndex = this.images.length - 1;
        this._imageIndex = newIndex;
    }
}

if (!customElements.get('image-view')) {
    customElements.define('image-view', ImageView);
}
