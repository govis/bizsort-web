import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('astro-company-profile')
export class AstroCompanyProfile extends LitElement {
  // We accept the data as a serialized JSON string from the server-rendered HTML attribute
  @property({ type: String, attribute: 'data-company' })
  declare dataCompany: string;

  // Internal state holding the parsed object
  declare company: any;

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if (changedProperties.has('dataCompany') && this.dataCompany) {
      try {
        this.company = JSON.parse(this.dataCompany);
      } catch (e) {
        console.error("Failed to parse company data", e);
      }
    }
  }

  static styles = css`
    .profile-card {
      border: 1px solid #e5e7eb;
      border-radius: 12px;
      padding: 2rem;
      margin: 2rem auto;
      max-width: 800px;
      font-family: system-ui, -apple-system, sans-serif;
      box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
      background: white;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      border-bottom: 1px solid #e5e7eb;
      padding-bottom: 1rem;
      margin-bottom: 1.5rem;
    }
    h1 {
      margin: 0 0 0.5rem 0;
      font-size: 2rem;
      color: #111827;
    }
    .category {
      display: inline-block;
      background: #f3f4f6;
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.875rem;
      color: #4b5563;
      font-weight: 500;
    }
    .description {
      color: #374151;
      line-height: 1.6;
    }
    .contact-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-top: 0.5rem;
      color: #2563eb;
      text-decoration: none;
    }
  `;

  render() {
    if (!this.company || !this.company.id) {
      return html`<div class="profile-card">Company not found.</div>`;
    }

    return html`
      <div class="profile-card">
        <div class="header">
          <div>
            <h1>${this.company.name}</h1>
            ${this.company.category ? html`<span class="category">${this.company.category.name}</span>` : ''}
          </div>
        </div>
        
        <div class="description">
          ${this.company.text || this.company.description || 'No description available.'}
        </div>

        <div style="margin-top: 2rem;">
          ${this.company.webSite ? html`
            <a href="${this.company.webSite}" target="_blank" class="contact-item">
              🌐 ${this.company.webSite}
            </a>
          ` : ''}
          
          ${this.company.email ? html`
            <a href="mailto:${this.company.email}" class="contact-item">
              ✉️ ${this.company.email}
            </a>
          ` : ''}
        </div>
      </div>
    `;
  }
}
