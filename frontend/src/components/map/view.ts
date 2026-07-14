import { LitElement, html, css } from 'lit';
import { property, query, state } from 'lit/decorators.js';
import type { Office } from '../types.js';
import '@awesome.me/webawesome/dist/components/dialog/dialog.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import type WaDialog from '@awesome.me/webawesome/dist/components/dialog/dialog.js';

export class MapView extends LitElement {
    @property({ type: Array })
    declare offices?: Office[];

    @property({ type: Object })
    declare office?: Office;

    @state()
    declare private _renderIframe: boolean;
    
    @query('wa-dialog')
    declare private _dialog: WaDialog;

    constructor() {
        super();
        this._renderIframe = false;
    }

    open(offices?: Office[]) {
        if (offices && offices.length) {
            if (offices.length === 1) {
                this.offices = undefined;
                this.office = offices[0];
            } else {
                this.office = undefined;
                this.offices = offices;
            }
            
            this._renderIframe = false; // reset
            if (this._dialog) {
                this._dialog.open = true;
            }
        } else {
            this.close();
        }
    }

    close() {
        if (this._dialog) {
            this._dialog.open = false;
        }
    }

    private _handleAfterShow() {
        // Once the dialog animation is perfectly finished, load the heavy iframe
        this._renderIframe = true;
    }

    private _handleAfterHide() {
        this.office = undefined;
        this.offices = undefined;
        this._renderIframe = false;
    }
    
    private _getOsmMapUrl(office?: Office) {
        if (!office?.location?.geoLocation) return '';
        const { lat, lng } = office.location.geoLocation;
        const offset = 0.01;
        const bbox = `${lng - offset},${lat - offset},${lng + offset},${lat + offset}`;
        return `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${lat},${lng}`;
    }

    static styles = css`
        :host {
            display: block;
        }

        wa-dialog {
            --width: 85vw;
            --body-spacing: 0;
        }

        .dialog-content {
            height: 85vh;
            display: flex;
            flex-direction: column;
            position: relative;
            background: #f8f9fa;
        }

        /* Floating 'X' button tucked cleanly inside the corner */
        .close-btn {
            position: absolute;
            top: 12px;
            right: 12px;
            z-index: 10;
            box-shadow: 0 4px 12px rgba(0,0,0,0.25);
            border-radius: 50%;
        }

        wa-button.close-btn {
            --wa-color-neutral-fill-quiet: #ffffff;
            --wa-color-neutral-fill-quiet-hover: #f5f5f5;
            --wa-color-neutral-on-quiet: #333;
        }

        .map-frame {
            width: 100%;
            height: 100%;
            border: none;
            flex-grow: 1;
        }

        .loading-placeholder {
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100%;
            width: 100%;
            color: #666;
            font-size: 1.2rem;
            font-family: inherit;
        }
        
        .multi-office-fallback {
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100%;
            width: 100%;
            color: #666;
            flex-direction: column;
            gap: 1rem;
            padding: 1rem;
            text-align: center;
        }

        @media (max-width: 768px) {
            wa-dialog {
                --width: 100vw;
            }
            .dialog-content {
                height: 100vh;
            }
        }
    `;

    render() {
        return html`
            <wa-dialog 
                without-header 
                @wa-after-show="${this._handleAfterShow}" 
                @wa-after-hide="${this._handleAfterHide}">
                
                <div class="dialog-content">
                    <wa-button 
                        is-icon-button 
                        pill 
                        class="close-btn" 
                        variant="neutral" 
                        @click="${this.close}"
                        title="Close map">
                        <wa-icon name="xmark"></wa-icon>
                    </wa-button>
                    
                    ${this._renderIframe && this.office ? html`
                        <iframe
                            class="map-frame"
                            src="${this._getOsmMapUrl(this.office)}"
                            scrolling="no"
                            marginheight="0"
                            marginwidth="0">
                        </iframe>
                    ` : this._renderIframe && this.offices ? html`
                        <div class="multi-office-fallback">
                            <h3>Multiple Offices</h3>
                            <p>Showing ${this.offices.length} offices.</p>
                        </div>
                    ` : html`
                        <div class="loading-placeholder">
                            Loading map...
                        </div>
                    `}
                </div>
            </wa-dialog>
        `;
    }
}

if (!customElements.get('map-view')) {
    customElements.define('map-view', MapView);
}
