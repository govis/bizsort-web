import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { repeat } from 'lit/directives/repeat.js';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';

// Shoelace components
import '@shoelace-style/shoelace/dist/components/card/card.js';
import '@shoelace-style/shoelace/dist/components/select/select.js';
import '@shoelace-style/shoelace/dist/components/option/option.js';
import '@shoelace-style/shoelace/dist/components/icon/icon.js';
import '@shoelace-style/shoelace/dist/components/button/button.js';
import '@shoelace-style/shoelace/dist/components/divider/divider.js';
import '@shoelace-style/shoelace/dist/components/spinner/spinner.js';

interface Office {
  id: number;
  name: string;
  phone: string;
  phone1?: string;
  fax?: string;
  address: string;
  latitude: number;
  longitude: number;
}

interface Company {
  id: number;
  name: string;
  email?: string;
  webSite?: string;
  description?: string;
  richText?: string;
  categoryName?: string;
  categoryId?: string;
  offices: Office[];
}

@customElement('company-profile')
export class CompanyProfile extends LitElement {
  @property({ type: Number }) companyId?: number;

  @state() private _company?: Company;
  @state() private _selectedOffice?: Office;
  @state() private _loading = false;
  @state() private _error?: string;

  static styles = css`
    :host {
      display: block;
      font-family: var(--sl-font-sans);
      color: var(--sl-color-neutral-900);
      max-width: 800px;
      margin: 2rem auto;
      padding: 0 1rem;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 4rem;
    }

    .profile-card {
      margin-bottom: 2rem;
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .company-name {
      font-size: var(--sl-font-size-2xl);
      font-weight: var(--sl-font-weight-bold);
      margin: 0;
    }

    .contact-grid {
      display: grid;
      grid-template-columns: 1fr;
      gap: 2rem;
    }

    @media (min-width: 600px) {
      .contact-grid {
        grid-template-columns: 1fr 1fr;
      }
    }

    .info-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .info-item {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      text-decoration: none;
      color: inherit;
    }

    .info-item sl-icon {
      font-size: 1.25rem;
      color: var(--sl-color-primary-600);
      margin-top: 0.2rem;
    }

    .map-container {
      border-radius: var(--sl-border-radius-medium);
      overflow: hidden;
      border: 1px solid var(--sl-color-neutral-200);
      aspect-ratio: 4 / 3;
      background-color: var(--sl-color-neutral-50);
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .map-image {
      width: 100%;
      height: 100%;
      object-fit: cover;
      cursor: pointer;
    }

    .about-section {
      margin-top: 2rem;
    }

    .about-title {
      font-size: var(--sl-font-size-xl);
      margin-bottom: 1rem;
    }

    .rich-text {
      line-height: 1.6;
    }

    .office-select {
      margin-bottom: 1.5rem;
    }
  `;

  async connectedCallback() {
    super.connectedCallback();
    if (this.companyId) {
      await this._fetchCompany();
    }
  }

  private async _fetchCompany() {
    this._loading = true;
    this._error = undefined;
    try {
      const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
      const response = await fetch(`${backendUrl}/api/company/profile/view?company=${this.companyId}`);
      if (!response.ok) throw new Error('Failed to fetch company');
      this._company = await response.json();
      this._selectedOffice = this._company?.offices[0];
    } catch (e: any) {
      this._error = e.message;
    } finally {
      this._loading = false;
    }
  }

  private _handleOfficeChange(e: CustomEvent) {
    const officeId = (e.target as any).value;
    this._selectedOffice = this._company?.offices.find(o => o.id.toString() === officeId);
  }

  private _getMapUrl(office: Office) {
    // Placeholder for Google Static Maps URL
    const apiKey = ''; // Should be passed via env
    return `https://maps.googleapis.com/maps/api/staticmap?size=600x450&maptype=roadmap&zoom=14&markers=${office.latitude},${office.longitude}&key=${apiKey}`;
  }

  render() {
    if (this._loading) {
      return html`
        <div class="loading-container">
          <sl-spinner style="font-size: 3rem;"></sl-spinner>
        </div>
      `;
    }

    if (this._error) {
      return html`<div style="color: var(--sl-color-danger-600);">Error: ${this._error}</div>`;
    }

    if (!this._company) return html`<div>Company not found.</div>`;

    return html`
      <div class="header">
        <h1 class="company-name">${this._company.name}</h1>
        ${this._company.categoryName ? html`
          <sl-button variant="text" size="small">
            <sl-icon slot="prefix" name="folder"></sl-icon>
            ${this._company.categoryName}
          </sl-button>
        ` : ''}
      </div>

      <sl-card class="profile-card">
        <div class="contact-grid">
          <div class="contact-info">
            ${this._company.offices.length > 1 ? html`
              <sl-select 
                label="Select Office" 
                value="${this._selectedOffice?.id.toString()}" 
                class="office-select"
                @sl-change="${this._handleOfficeChange}"
              >
                ${repeat(this._company.offices, (o) => o.id, (o) => html`
                  <sl-option value="${o.id.toString()}">${o.name}</sl-option>
                `)}
              </sl-select>
            ` : ''}

            <div class="info-list">
              ${this._selectedOffice ? html`
                <div class="info-item">
                  <sl-icon name="geo-alt"></sl-icon>
                  <span>${this._selectedOffice.address}</span>
                </div>
                <div class="info-item">
                  <sl-icon name="telephone"></sl-icon>
                  <span>${this._selectedOffice.phone}${this._selectedOffice.phone1 ? `, ${this._selectedOffice.phone1}` : ''}</span>
                </div>
                ${this._selectedOffice.fax ? html`
                  <div class="info-item">
                    <sl-icon name="printer"></sl-icon>
                    <span>${this._selectedOffice.fax}</span>
                  </div>
                ` : ''}
              ` : ''}

              ${this._company.email ? html`
                <a href="mailto:${this._company.email}" class="info-item">
                  <sl-icon name="envelope"></sl-icon>
                  <span>${this._company.email}</span>
                </a>
              ` : ''}

              ${this._company.webSite ? html`
                <a href="${this._company.webSite}" target="_blank" class="info-item">
                  <sl-icon name="box-arrow-up-right"></sl-icon>
                  <span>${this._company.webSite}</span>
                </a>
              ` : ''}
            </div>
          </div>

          <div class="map-container">
            ${this._selectedOffice ? html`
              <img 
                src="${this._getMapUrl(this._selectedOffice)}" 
                alt="Map" 
                class="map-image"
                @click="${() => window.open(`https://www.google.com/maps/search/?api=1&query=${this._selectedOffice?.latitude},${this._selectedOffice?.longitude}`)}"
              />
            ` : html`<span>Map not available</span>`}
          </div>
        </div>
      </sl-card>

      <div class="about-section">
        <h2 class="about-title">About ${this._company.name}</h2>
        <sl-divider></sl-divider>
        <div class="rich-text">
          ${this._company.richText ? unsafeHTML(this._company.richText) : this._company.description}
        </div>
      </div>
    `;
  }
}
