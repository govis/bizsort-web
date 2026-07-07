import { LitElement, html, css } from 'lit';
import { property, query } from 'lit/decorators.js';
import type { Office } from '../types.js';
import '@awesome.me/webawesome/dist/components/dialog/dialog.js';
import '@awesome.me/webawesome/dist/components/button/button.js';

export class MapView extends LitElement {
    @property({ type: Array })
    declare offices?: Office[];

    @property({ type: Object })
    declare office?: Office;
    
    @query('wa-dialog')
    declare dialog: any;

    open(offices?: Office[]) {
        if (offices && offices.length) {
            if (offices.length === 1) {
                this.offices = undefined;
                this.office = offices[0];
            } else {
                this.office = undefined;
                this.offices = offices;
            }
            this.dialog.show();
        } else {
            this.office = undefined;
            this.offices = undefined;
        }
    }

    close() {
        this.dialog.hide();
    }
    
    private _getOsmMapUrl(office?: Office) {
        if (!office?.location?.geoLocation) return '';
        const { lat, lng } = office.location.geoLocation;
        const offset = 0.01;
        const bbox = `${lng - offset},${lat - offset},${lng + offset},${lat + offset}`;
        return `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${lat},${lng}`;
    }

    static styles = css`
        wa-dialog::part(panel) {
            width: 90vw;
            height: 90vh;
            max-width: 1200px;
            max-height: 800px;
            display: flex;
            flex-direction: column;
        }
        wa-dialog::part(body) {
            padding: 0;
            flex: 1;
            display: flex;
            flex-direction: column;
        }
        .map-frame {
            width: 100%;
            height: 100%;
            border: none;
            flex-grow: 1;
        }
        .multi-office-fallback {
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100%;
            color: #666;
            flex-direction: column;
            gap: 1rem;
        }
    `;

    render() {
        return html`
            <wa-dialog label="${this.office ? this.office.name || 'Office Map' : 'Offices Map'}">
                ${this.office ? html`
                    <iframe
                        class="map-frame"
                        src="${this._getOsmMapUrl(this.office)}"
                        scrolling="no"
                        marginheight="0"
                        marginwidth="0">
                    </iframe>
                ` : this.offices ? html`
                    <div class="multi-office-fallback">
                        <h3>Multiple Offices Map</h3>
                        <p>Showing ${this.offices.length} offices. (OpenStreetMap embed supports single markers by default).</p>
                    </div>
                ` : ''}
                <wa-button slot="footer" variant="primary" @click="${this.close}">Close</wa-button>
            </wa-dialog>
        `;
    }
}

if (!customElements.get('map-view')) {
    customElements.define('map-view', MapView);
}
