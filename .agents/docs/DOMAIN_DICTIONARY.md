# BizSort Domain Dictionary

This document tracks the evolving domain terminology and architectural concepts of the BizSort project. It serves as a translation guide between the legacy architecture and the modernized application.

## Core Entities & Concepts

| Modern Term | Legacy Term | Description / Rationale |
| :--- | :--- | :--- |
| **Company** | *Business* | The core entity of the platform. Represents an organization. Renamed from "Business" (e.g., `Businesses` -> `CompanyProfiles`, `biz` -> `cm`) to provide a clearer, more standard B2B domain terminology and avoid variable naming confusion. |
| **Offering** | *Product* | A generic term representing anything a Company sells or provides. Can be either a physical/digital **Product** or a **Service**. Every offering is strictly associated with a parent Company. *(Note: Codebase refactoring from Product -> Offering is complete).* |
| **Group** | *Group* | A shared foundational namespace (`BizSrt.Model.Group`) used to represent hierarchical or categorizational tree structures. Currently powers both the `Category` hierarchy and the `Location` hierarchy, as they share the same underlying tree traversal and caching logic. |

## Application Contexts

| Concept | Description |
| :--- | :--- |
| **Company Profile Page** | The main landing page for a Company. Displays aggregate information, including lightweight preview cards of the company's Offerings. |
| **Offering Page** | A dedicated page for a specific Offering (formerly Product). Displays the full, detailed profile of that specific item, still tied to its parent company. |

*(Add new terms here as the domain language evolves)*
