import { LitElement, html } from 'lit';
import { provide } from '@lit/context';
import { companyContext } from './context.js';
import { view } from '../service/company.js';

export class CompanyProvider extends LitElement {
  static get properties() {
    return {
      company: { type: Object },
      companyId: { type: Number, attribute: 'company-id' }
    };
  }

  @provide({ context: companyContext })
  declare company: any;

  declare companyId?: number;

  willUpdate(changed: Map<string, unknown>) {
    if (changed.has('companyId') && !this.company && this.companyId) {
       view(this.companyId).then(data => {
           this.company = data;
       }).catch(err => {
           console.error("Failed to fetch company context", err);
       });
    }
  }

  render() {
    return html`<slot></slot>`;
  }
}

if (!customElements.get('company-provider')) {
  customElements.define('company-provider', CompanyProvider);
}
