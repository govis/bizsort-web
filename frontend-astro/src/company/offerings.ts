import { LitElement, html, css } from 'lit';
import { property } from 'lit/decorators.js';
import { View } from '../viewmodel/list/view';
import type { IViewAdapter } from '../viewmodel';
import type { Action } from '../global';
import { getOfferings } from '../service/company';
import { toPreview } from '../service/offering';
import './header-layout';
import '../components/offering/listview';
import '../components/list/header';
import '../components/list/pager';

class CompanyOfferingsViewModel extends View {
  declare companyId: number;

  fetchList(queryInput: any, callback: Action<any>, faultCallback: Action<any>) {
    if (!this.companyId) {
      faultCallback(new Error("No company ID"));
      return;
    }
    
    // Pass queryInput (which contains paging/search parameters from the view)
    getOfferings(this.companyId, queryInput).then(callback).catch(faultCallback);
  }

  fetchPage(page: any[], fetchAction: Action<Object[]>, faultCallback: Action<any>): void {
    if (!page || page.length === 0) {
      fetchAction([]);
      return;
    }
    toPreview(page, { company: this.companyId }).then(fetchAction).catch(faultCallback);
  }
}

export class CompanyOfferings extends LitElement implements IViewAdapter {
  @property({ type: Object })
  declare company: any;
  
  @property({ type: Array, attribute: 'initial-items' })
  declare initialItems: any[];

  viewModel: CompanyOfferingsViewModel;
  
  modelUpdated(_props: string[]) {
    this.requestUpdate();
  }

  constructor() {
    super();
    this.viewModel = new CompanyOfferingsViewModel(this);
    this.viewModel.pager.pageSizes = [12, 24, 48]; 
  }

  firstUpdated() {
    this.viewModel.companyId = this.company?.id;
    this.viewModel.initialize();
    
    // If we received SSR items, we don't need to fetch initially
    if (this.initialItems && this.initialItems.length > 0) {
      // We manually seed the pager/list if we want, or just trigger search() to fetch the true state
      // For simplicity, just search to get total counts from the backend if SSR didn't provide full pager context
      this.viewModel.search();
    } else if (this.company?.id) {
      this.viewModel.search();
    }
  }

  getViewModel(name: string) {
    if (name === 'listView') return this.shadowRoot?.querySelector('offering-listview');
    if (name === 'listHeader') return this.shadowRoot?.querySelector('list-header');
    return null;
  }

  static styles = css`
    .company-profile-content {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem 1rem;
      display: flex;
      flex-direction: column;
      min-height: 50vh;
      gap: 1rem;
    }
    
    wa-tab-group {
      width: 100%;
    }
    wa-tab-group::part(body) {
      display: none;
    }
    wa-tab {
      --wa-color-neutral-on-quiet: rgba(255, 255, 255, 0.7);
      --wa-color-brand-on-quiet: white;
    }
  `;
  
  render() {
    if (!this.company) return html``;
    
    return html`
      <company-header-layout active-tab="offerings">
        <div class="company-profile-content">
          <list-header entity="offering"></list-header>
          
          <div class="content vertical layout flex">
            <offering-listview id="listView" list noCategory></offering-listview>
            <div class="flex"></div>
            <list-pager class="self-center" .master=${this.viewModel.pager}></list-pager>
          </div>
        </div>
      </company-header-layout>
    `;
  }
}

if (!customElements.get('company-offerings')) {
  customElements.define('company-offerings', CompanyOfferings);
}
