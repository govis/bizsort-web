<template>
  <div>
    <!-- 
      Nuxt can natively render Web Components!
      Notice there is NO React wrapper, NO ClientLayout, NO bundle.tsx!
      We pass the data as a serialized attribute to bridge the Server/Client gap.
    -->
    <company-profile
      v-if="company"
      :company="JSON.stringify(company)"
      :active-tab="activeTab"
    ></company-profile>

    <div v-else-if="error" style="color: red; padding: 2rem;">
      Error fetching company profile.
    </div>
  </div>
</template>

<script setup>
import { useRoute, useAsyncData, useSeoMeta } from '#imports'

const route = useRoute()
const companyId = parseInt(route.params.id, 10)
const activeTab = route.query.tab || 'about'

// Bypass local SSL for node fetch in development
if (process.server) {
  process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'
}

// 1. Fetch data on the Server
// useAsyncData automatically handles SSR deduplication and hydration!
const { data: company, error } = await useAsyncData(`company-${companyId}`, async () => {
  const API_BASE = "https://localhost:5001"
  const response = await fetch(`${API_BASE}/api/company/profile/view?company=${companyId}`)
  
  if (!response.ok) {
    throw new Error('Failed to fetch company profile')
  }
  
  return await response.json()
})

// 2. Dynamic SEO Metadata Generation
if (company.value) {
  const title = company.value.category 
    ? `${company.value.name} - ${company.value.category.name} | BizSort (Nuxt)`
    : `${company.value.name} | BizSort (Nuxt)`

  useSeoMeta({
    title: title,
    description: company.value.text || `View detailed company profile for ${company.value.name}.`
  })
}

// 3. Client-Side Only Import of the Lit Component
if (process.client) {
  import('~frontend/company/profile.ts')
}
</script>
