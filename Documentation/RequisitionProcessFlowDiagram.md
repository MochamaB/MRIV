# Requisition Process Flow Diagram

This diagram visualizes the step-by-step flow of the requisition process, including wizard steps, branching by requisition type, and integration with approval and material logic.

```mermaid
flowchart TD
    A["Start Requisition Wizard"] --> B["Select Ticket (optional)"]
    B --> C["Enter Requisition Details"]
    C --> D["Add Requisition Items"]
    D --> E["Generate Approval Steps"]
    E --> F["Select Approvers/Receivers"]
    F --> G["Review Summary"]
    G --> H["Submit Requisition"]
    H --> I{"Requisition Type"}
    I -->|"New Purchase"| J1["Create New Material"]
    I -->|"Transfer"| J2["Update Material Assignment"]
    I -->|"InterFactory"| J3["Validate Both Factories"]
    I -->|"Maintenance"| J4["Set Material Under Maintenance"]
    I -->|"Return"| J5["Close Assignment, Set Available"]
    I -->|"Disposal"| J6["Set Disposed, Audit Trail"]
    I -->|"Loan/Borrow"| J7["Create Time-bound Assignment"]
    I -->|"Temporary Allocation"| J8["Short-term Assignment"]
    I -->|"Other"| J9["Custom Workflow"]
    J1 --> K["Approval Workflow"]
    J2 --> K
    J3 --> K
    J4 --> K
    J5 --> K
    J6 --> K
    J7 --> K
    J8 --> K
    J9 --> K
    K --> L{"Approval Step"}
    L -->|"Approved"| M["Update Material/Assignment/Status"]
    L -->|"Rejected"| N["Notify Requester, Update Status"]
    M --> O["Send Notifications"]
    N --> O
    O --> P["Requisition Complete/Closed"]
    style I fill:#f9f,stroke:#333,stroke-width:2px
    style K fill:#bbf,stroke:#333,stroke-width:2px
    style L fill:#bbf,stroke:#333,stroke-width:2px
    style M fill:#bfb,stroke:#333,stroke-width:2px
    style N fill:#fbb,stroke:#333,stroke-width:2px
    style P fill:#ccc,stroke:#333,stroke-width:2px
``` 