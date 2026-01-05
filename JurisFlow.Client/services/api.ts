import { Matter, Task, TimeEntry, Lead, CalendarEvent, Invoice, TaskStatus, Expense, Employee } from "../types";

// Use relative path when in browser (proxy will handle it), fallback to full URL for SSR
const API_URL = typeof window !== 'undefined' ? '/api' : 'http://localhost:3001/api';

const fetchJson = async (endpoint: string, options: RequestInit = {}) => {
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/468b8283-de18-4f31-b7cb-52da7f0bb927', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ location: 'api.ts:6', message: 'API call started', data: { endpoint, method: options.method || 'GET' }, timestamp: Date.now(), sessionId: 'debug-session', runId: 'run1', hypothesisId: 'A' }) }).catch(() => { });
    // #endregion
    const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null;
    const res = await fetch(`${API_URL}${endpoint}`, {
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {})
        },
        cache: 'no-store',
        ...options
    });
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/468b8283-de18-4f31-b7cb-52da7f0bb927', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ location: 'api.ts:15', message: 'API response received', data: { endpoint, status: res.status, statusText: res.statusText, hasToken: !!token }, timestamp: Date.now(), sessionId: 'debug-session', runId: 'run1', hypothesisId: 'A' }) }).catch(() => { });
    // #endregion
    // Handle 401 Unauthorized gracefully - return null instead of throwing
    if (res.status === 401) {
        // #region agent log
        fetch('http://127.0.0.1:7242/ingest/468b8283-de18-4f31-b7cb-52da7f0bb927', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ location: 'api.ts:18', message: 'API 401 Unauthorized', data: { endpoint }, timestamp: Date.now(), sessionId: 'debug-session', runId: 'run1', hypothesisId: 'A' }) }).catch(() => { });
        // #endregion
        return null;
    }
    if (!res.ok) {
        // #region agent log
        fetch('http://127.0.0.1:7242/ingest/468b8283-de18-4f31-b7cb-52da7f0bb927', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ location: 'api.ts:22', message: 'API error', data: { endpoint, status: res.status, statusText: res.statusText }, timestamp: Date.now(), sessionId: 'debug-session', runId: 'run1', hypothesisId: 'A' }) }).catch(() => { });
        // #endregion
        throw new Error(`API Error: ${res.statusText}`);
    }
    // #region agent log
    fetch('http://127.0.0.1:7242/ingest/468b8283-de18-4f31-b7cb-52da7f0bb927', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ location: 'api.ts:25', message: 'API call succeeded', data: { endpoint }, timestamp: Date.now(), sessionId: 'debug-session', runId: 'run1', hypothesisId: 'A' }) }).catch(() => { });
    // #endregion
    // Handle 204 No Content responses (common for DELETE operations)
    if (res.status === 204 || res.headers.get('content-length') === '0') {
        return null;
    }
    return res.json();
};

export const api = {
    // Auth
    login: (data: { email: string; password: string }) => fetchJson('/login', { method: 'POST', body: JSON.stringify(data) }),

    // Matters
    getMatters: () => fetchJson('/matters'),
    createMatter: (data: Partial<Matter>) => fetchJson('/matters', { method: 'POST', body: JSON.stringify(data) }),
    updateMatter: (id: string, data: Partial<Matter>) => fetchJson(`/matters/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteMatter: (id: string) => fetchJson(`/matters/${id}`, { method: 'DELETE' }),

    // Tasks
    getTasks: () => fetchJson('/tasks'),
    createTask: (data: Partial<Task>) => fetchJson('/tasks', { method: 'POST', body: JSON.stringify(data) }),
    updateTaskStatus: (id: string, status: TaskStatus) => fetchJson(`/tasks/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),
    updateTask: (id: string, data: Partial<Task>) => fetchJson(`/tasks/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteTask: (id: string) => fetchJson(`/tasks/${id}`, { method: 'DELETE' }),

    // Task Templates
    getTaskTemplates: () => fetchJson('/task-templates'),
    createTasksFromTemplate: (data: { templateId: string; matterId?: string; assignedTo?: string; baseDate?: string }) =>
        fetchJson('/tasks/from-template', { method: 'POST', body: JSON.stringify(data) }),

    // Time & Expenses
    getTimeEntries: () => fetchJson('/time-entries'),
    createTimeEntry: (data: Partial<TimeEntry>) => fetchJson('/time-entries', { method: 'POST', body: JSON.stringify(data) }),
    getExpenses: () => fetchJson('/expenses'),
    createExpense: (data: Partial<Expense>) => fetchJson('/expenses', { method: 'POST', body: JSON.stringify(data) }),
    markAsBilled: (matterId: string) => fetchJson('/billing/mark-billed', { method: 'POST', body: JSON.stringify({ matterId }) }),

    // CRM
    getClients: () => fetchJson('/clients'),
    createClient: (data: any) => fetchJson('/clients', { method: 'POST', body: JSON.stringify(data) }),
    setClientPassword: (id: string, password: string) =>
        fetchJson(`/clients/${id}/set-password`, { method: 'POST', body: JSON.stringify({ password }) }),
    getLeads: () => fetchJson('/leads'),
    createLead: (data: Partial<Lead>) => fetchJson('/leads', { method: 'POST', body: JSON.stringify(data) }),
    updateLead: (id: string, data: Partial<Lead>) => fetchJson(`/leads/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteLead: (id: string) => fetchJson(`/leads/${id}`, { method: 'DELETE' }),

    // Opposing Parties
    getOpposingParties: () => fetchJson('/opposingparties'),
    getOpposingPartiesByMatter: (matterId: string) => fetchJson(`/opposingparties/matter/${matterId}`),
    createOpposingParty: (data: any) => fetchJson('/opposingparties', { method: 'POST', body: JSON.stringify(data) }),
    updateOpposingParty: (id: string, data: any) => fetchJson(`/opposingparties/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteOpposingParty: (id: string) => fetchJson(`/opposingparties/${id}`, { method: 'DELETE' }),

    // Calendar
    getEvents: () => fetchJson('/events'),
    createEvent: (data: Partial<CalendarEvent>) => fetchJson('/events', { method: 'POST', body: JSON.stringify(data) }),
    deleteEvent: (id: string) => fetchJson(`/events/${id}`, { method: 'DELETE' }),

    // Invoices
    getInvoices: () => fetchJson('/invoices'),
    getInvoice: (id: string) => fetchJson(`/invoices/${id}`),
    createInvoice: (data: any) => {
        const payload = { ...data, clientId: data.client?.id || data.clientId };
        return fetchJson('/invoices', { method: 'POST', body: JSON.stringify(payload) });
    },
    updateInvoice: (id: string, data: any) => fetchJson(`/invoices/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteInvoice: (id: string) => fetchJson(`/invoices/${id}`, { method: 'DELETE' }),

    // Invoice Workflow
    approveInvoice: (id: string) => fetchJson(`/invoices/${id}/approve`, { method: 'POST' }),
    sendInvoice: (id: string) => fetchJson(`/invoices/${id}/send`, { method: 'POST' }),

    // Invoice Line Items
    addInvoiceLineItem: (invoiceId: string, data: any) => fetchJson(`/invoices/${invoiceId}/line-items`, { method: 'POST', body: JSON.stringify(data) }),
    updateInvoiceLineItem: (invoiceId: string, itemId: string, data: any) => fetchJson(`/invoices/${invoiceId}/line-items/${itemId}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteInvoiceLineItem: (invoiceId: string, itemId: string) => fetchJson(`/invoices/${invoiceId}/line-items/${itemId}`, { method: 'DELETE' }),

    // Invoice Payments
    recordPayment: (invoiceId: string, data: any) => fetchJson(`/invoices/${invoiceId}/payments`, { method: 'POST', body: JSON.stringify(data) }),
    refundPayment: (invoiceId: string, paymentId: string, data: any) => fetchJson(`/invoices/${invoiceId}/payments/${paymentId}/refund`, { method: 'POST', body: JSON.stringify(data) }),

    // Notifications
    getNotifications: (userId?: string) => fetchJson(`/notifications${userId ? `?userId=${encodeURIComponent(userId)}` : ''}`),
    markNotificationRead: (id: string) => fetchJson(`/notifications/${id}/read`, { method: 'POST' }),
    markNotificationUnread: (id: string) => fetchJson(`/notifications/${id}/unread`, { method: 'POST' }),
    markAllNotificationsRead: () => fetchJson('/notifications/read-all', { method: 'POST' }),

    // Reports
    getReportOverview: (params: { from?: string; to?: string; matterId?: string } = {}) => {
        const qs = new URLSearchParams();
        Object.entries(params).forEach(([k, v]) => {
            if (!v) return;
            qs.set(k, v);
        });
        const query = qs.toString() ? `?${qs.toString()}` : '';
        return fetchJson(`/reports/overview${query}`);
    },

    // User Profile
    updateUserProfile: (data: any) => fetchJson('/user/profile', { method: 'PUT', body: JSON.stringify(data) }),

    // Admin: User Management
    admin: {
        getUsers: () => fetchJson('/admin/users'),
        createUser: (data: any) => fetchJson('/admin/users', { method: 'POST', body: JSON.stringify(data) }),
        updateUser: (id: string, data: any) => fetchJson(`/admin/users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
        deleteUser: (id: string) => fetchJson(`/admin/users/${id}`, { method: 'DELETE' }),

        // Admin: Client Management
        getClients: () => fetchJson('/admin/clients'),
        updateClient: (id: string, data: any) => fetchJson(`/admin/clients/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
        deleteClient: (id: string) => fetchJson(`/admin/clients/${id}`, { method: 'DELETE' }),

        // Admin: Audit Logs
        getAuditLogs: (params: {
            page?: number;
            limit?: number;
            action?: string;
            entityType?: string;
            entityId?: string;
            userId?: string;
            clientId?: string;
            email?: string;
            q?: string;
            from?: string;
            to?: string;
        } = {}) => {
            const qs = new URLSearchParams();
            Object.entries(params).forEach(([k, v]) => {
                if (v === undefined || v === null || v === '') return;
                qs.set(k, String(v));
            });
            const query = qs.toString() ? `?${qs.toString()}` : '';
            return fetchJson(`/admin/audit-logs${query}`);
        }
    },

    // Documents
    uploadDocument: async (file: File, matterId?: string, description?: string) => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null;
        const formData = new FormData();
        formData.append('file', file);
        if (matterId) formData.append('matterId', matterId);
        if (description) formData.append('description', description);

        const res = await fetch(`${API_URL}/documents/upload`, {
            method: 'POST',
            headers: {
                ...(token ? { Authorization: `Bearer ${token}` } : {})
            },
            body: formData
        });
        if (res.status === 401) return null;
        if (!res.ok) throw new Error(`API Error: ${res.statusText}`);
        return res.json();
    },
    getDocuments: (params?: { matterId?: string; q?: string }) => {
        const qs = new URLSearchParams();
        if (params?.matterId) qs.set('matterId', params.matterId);
        if (params?.q) qs.set('q', params.q);
        const query = qs.toString() ? `?${qs.toString()}` : '';
        return fetchJson(`/documents${query}`);
    },
    searchDocuments: (q: string, options?: { matterId?: string; includeContent?: boolean }) => {
        const qs = new URLSearchParams({ q });
        if (options?.matterId) qs.set('matterId', options.matterId);
        if (options?.includeContent) qs.set('includeContent', 'true');
        return fetchJson(`/documents/search?${qs.toString()}`);
    },
    deleteDocument: (id: string) => fetchJson(`/documents/${id}`, { method: 'DELETE' }),
    updateDocument: (id: string, data: { matterId?: string | null; description?: string | null; tags?: string[] | string | null; category?: string | null }) =>
        fetchJson(`/documents/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    bulkAssignDocuments: (data: { ids: string[]; matterId?: string | null }) =>
        fetchJson('/documents/bulk-assign', { method: 'PUT', body: JSON.stringify(data) }),

    // Password Reset
    forgotPassword: (email: string, userType: 'attorney' | 'client') =>
        fetchJson('/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email, userType }) }),
    resetPassword: (token: string, password: string) =>
        fetchJson('/auth/reset-password', { method: 'POST', body: JSON.stringify({ token, password }) }),

    // ========== V2.0 APIs ==========

    // Trust Accounting
    getTrustTransactions: (matterId: string) => fetchJson(`/matters/${matterId}/trust`),
    createTrustTransaction: (matterId: string, data: { type: string; amount: number; description: string; reference?: string }) =>
        fetchJson(`/matters/${matterId}/trust`, { method: 'POST', body: JSON.stringify(data) }),

    // Workflows
    getWorkflows: () => fetchJson('/workflows'),
    createWorkflow: (data: { name: string; description?: string; trigger: string; actions: any[]; isActive?: boolean }) =>
        fetchJson('/workflows', { method: 'POST', body: JSON.stringify(data) }),
    updateWorkflow: (id: string, data: Partial<{ name: string; description: string; trigger: string; actions: any[]; isActive: boolean }>) =>
        fetchJson(`/workflows/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteWorkflow: (id: string) => fetchJson(`/workflows/${id}`, { method: 'DELETE' }),

    // Appointments (Attorney)
    getAppointments: () => fetchJson('/appointments'),
    updateAppointment: (id: string, data: { status: string; approvedDate?: string; assignedTo?: string }) =>
        fetchJson(`/appointments/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

    // Intake Forms
    getIntakeForms: () => fetchJson('/intake-forms'),
    createIntakeForm: (data: { name: string; description?: string; fields: any[]; practiceArea?: string }) =>
        fetchJson('/intake-forms', { method: 'POST', body: JSON.stringify(data) }),
    getIntakeSubmissions: () => fetchJson('/intake-submissions'),
    updateIntakeSubmission: (id: string, data: { status: string; notes?: string }) =>
        fetchJson(`/intake-submissions/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

    // Settlement Statements
    getSettlementStatements: (matterId: string) => fetchJson(`/matters/${matterId}/settlement`),
    createSettlementStatement: (matterId: string, data: { grossSettlement: number; attorneyFees: number; expenses: number; liens?: number }) =>
        fetchJson(`/matters/${matterId}/settlement`, { method: 'POST', body: JSON.stringify(data) }),

    // Signature Requests
    createSignatureRequest: (documentId: string, data: { clientId: string; expiresAt?: string }) =>
        fetchJson(`/documents/${documentId}/signature`, { method: 'POST', body: JSON.stringify(data) }),
    getDocumentSignatures: (documentId: string) => fetchJson(`/documents/${documentId}/signatures`),

    // Employees
    getEmployees: () => fetchJson('/employees'),
    getEmployee: (id: string) => fetchJson(`/employees/${id}`),
    createEmployee: (data: Partial<Employee>) => fetchJson('/employees', { method: 'POST', body: JSON.stringify(data) }),
    updateEmployee: (id: string, data: Partial<Employee>) => fetchJson(`/employees/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteEmployee: (id: string) => fetchJson(`/employees/${id}`, { method: 'DELETE' }),
    resetEmployeePassword: (id: string) => fetchJson(`/employees/${id}/reset-password`, { method: 'POST' }),
    assignTaskToEmployee: (employeeId: string, taskId: string) => fetchJson(`/employees/${employeeId}/assign-task`, { method: 'POST', body: JSON.stringify({ taskId }) }),
    uploadEmployeeAvatar: async (employeeId: string, file: File) => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null;
        const formData = new FormData();
        formData.append('file', file);
        const res = await fetch(`${API_URL}/employees/${employeeId}/avatar`, {
            method: 'POST',
            headers: {
                ...(token ? { Authorization: `Bearer ${token}` } : {})
            },
            body: formData
        });
        if (res.status === 401) return null;
        if (!res.ok) throw new Error(`API Error: ${res.statusText}`);
        return res.json();
    },

    // Conflict Checking
    conflicts: {
        check: (data: { searchQuery: string; checkType?: string; entityType?: string; entityId?: string }) =>
            fetchJson('/conflicts/check', { method: 'POST', body: JSON.stringify(data) }),
        get: (id: string) => fetchJson(`/conflicts/${id}`),
        waive: (id: string, reason: string) => fetchJson(`/conflicts/${id}/waive`, { method: 'POST', body: JSON.stringify({ reason }) }),
        history: (limit?: number) => fetchJson(`/conflicts/history${limit ? `?limit=${limit}` : ''}`),
    },

    // E-Signatures
    signatures: {
        request: (data: { documentId: string; signerEmail: string; signerName?: string; clientId?: string; expiresAt?: string }) =>
            fetchJson('/signatures/request', { method: 'POST', body: JSON.stringify(data) }),
        get: (id: string) => fetchJson(`/signatures/${id}`),
        getByDocument: (documentId: string) => fetchJson(`/signatures/document/${documentId}`),
        getByMatter: (matterId: string) => fetchJson(`/signatures/matter/${matterId}`),
        sign: (id: string) => fetchJson(`/signatures/${id}/sign`, { method: 'POST' }),
        decline: (id: string, reason?: string) => fetchJson(`/signatures/${id}/decline`, { method: 'POST', body: JSON.stringify({ reason }) }),
        remind: (id: string) => fetchJson(`/signatures/${id}/remind`, { method: 'POST' }),
        void: (id: string) => fetchJson(`/signatures/${id}/void`, { method: 'POST' }),
    },

    // Online Payments
    payments: {
        createCheckout: (data: { invoiceId?: string; matterId?: string; clientId?: string; amount: number; currency?: string; payerEmail?: string; payerName?: string }) =>
            fetchJson('/payments/create-checkout', { method: 'POST', body: JSON.stringify(data) }),
        get: (id: string) => fetchJson(`/payments/${id}`),
        getByInvoice: (invoiceId: string) => fetchJson(`/payments/invoice/${invoiceId}`),
        getByMatter: (matterId: string) => fetchJson(`/payments/matter/${matterId}`),
        getByClient: (clientId: string) => fetchJson(`/payments/client/${clientId}`),
        complete: (id: string, data: { externalTransactionId?: string; cardLast4?: string; cardBrand?: string; receiptUrl?: string }) =>
            fetchJson(`/payments/${id}/complete`, { method: 'POST', body: JSON.stringify(data) }),
        fail: (id: string, reason?: string) => fetchJson(`/payments/${id}/fail`, { method: 'POST', body: JSON.stringify({ reason }) }),
        refund: (id: string, data: { amount?: number; reason?: string }) =>
            fetchJson(`/payments/${id}/refund`, { method: 'POST', body: JSON.stringify(data) }),
        stats: (from?: string, to?: string) => fetchJson(`/payments/stats${from || to ? `?from=${from || ''}&to=${to || ''}` : ''}`),
    },

    // Deadlines
    deadlines: {
        list: (params?: { matterId?: string; status?: string; days?: number }) => {
            const query = new URLSearchParams();
            if (params?.matterId) query.append('matterId', params.matterId);
            if (params?.status) query.append('status', params.status);
            if (params?.days) query.append('days', params.days.toString());
            return fetchJson(`/deadlines?${query.toString()}`);
        },
        get: (id: string) => fetchJson(`/deadlines/${id}`),
        create: (data: { matterId: string; title: string; dueDate: string; description?: string; priority?: string; deadlineType?: string; assignedTo?: string }) =>
            fetchJson('/deadlines', { method: 'POST', body: JSON.stringify(data) }),
        update: (id: string, data: { title?: string; description?: string; dueDate?: string; status?: string; priority?: string }) =>
            fetchJson(`/deadlines/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
        complete: (id: string) => fetchJson(`/deadlines/${id}/complete`, { method: 'POST' }),
        delete: (id: string) => fetchJson(`/deadlines/${id}`, { method: 'DELETE' }),
        upcoming: (days?: number) => fetchJson(`/deadlines/upcoming${days ? `?days=${days}` : ''}`),
        calculate: (data: { courtRuleId: string; triggerDate?: string; serviceMethod?: string }) =>
            fetchJson('/deadlines/calculate', { method: 'POST', body: JSON.stringify(data) }),
    },

    // Court Rules
    courtRules: {
        list: (params?: { jurisdiction?: string; ruleType?: string; triggerEvent?: string }) => {
            const query = new URLSearchParams();
            if (params?.jurisdiction) query.append('jurisdiction', params.jurisdiction);
            if (params?.ruleType) query.append('ruleType', params.ruleType);
            if (params?.triggerEvent) query.append('triggerEvent', params.triggerEvent);
            return fetchJson(`/court-rules?${query.toString()}`);
        },
        get: (id: string) => fetchJson(`/court-rules/${id}`),
        create: (data: any) => fetchJson('/court-rules', { method: 'POST', body: JSON.stringify(data) }),
        update: (id: string, data: any) => fetchJson(`/court-rules/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
        delete: (id: string) => fetchJson(`/court-rules/${id}`, { method: 'DELETE' }),
        jurisdictions: () => fetchJson('/court-rules/jurisdictions'),
        triggerEvents: (jurisdiction?: string) => fetchJson(`/court-rules/trigger-events${jurisdiction ? `?jurisdiction=${jurisdiction}` : ''}`),
        seed: () => fetchJson('/court-rules/seed', { method: 'POST' }),
    },

    // Email Sync
    emails: {
        list: (params?: { matterId?: string; clientId?: string; folder?: string; limit?: number }) => {
            const query = new URLSearchParams();
            if (params?.matterId) query.append('matterId', params.matterId);
            if (params?.clientId) query.append('clientId', params.clientId);
            if (params?.folder) query.append('folder', params.folder);
            if (params?.limit) query.append('limit', params.limit.toString());
            return fetchJson(`/emails?${query.toString()}`);
        },
        get: (id: string) => fetchJson(`/emails/${id}`),
        link: (id: string, data: { matterId?: string; clientId?: string }) =>
            fetchJson(`/emails/${id}/link`, { method: 'POST', body: JSON.stringify(data) }),
        unlink: (id: string) => fetchJson(`/emails/${id}/unlink`, { method: 'POST' }),
        unlinked: (limit?: number) => fetchJson(`/emails/unlinked${limit ? `?limit=${limit}` : ''}`),
        autoLink: () => fetchJson('/emails/auto-link', { method: 'POST' }),
        // Accounts
        accounts: {
            list: () => fetchJson('/emails/accounts'),
            connectOutlook: (data: { email: string; displayName?: string; accessToken?: string; refreshToken?: string }) =>
                fetchJson('/emails/accounts/connect/outlook', { method: 'POST', body: JSON.stringify(data) }),
            connectGmail: (data: { email: string; displayName?: string; accessToken?: string; refreshToken?: string }) =>
                fetchJson('/emails/accounts/connect/gmail', { method: 'POST', body: JSON.stringify(data) }),
            sync: (id: string) => fetchJson(`/emails/accounts/${id}/sync`, { method: 'POST' }),
            disconnect: (id: string) => fetchJson(`/emails/accounts/${id}`, { method: 'DELETE' }),
        },
    },

    // SMS Messaging (Twilio)
    sms: {
        send: (data: { toNumber: string; body: string; matterId?: string; clientId?: string; templateId?: string }) =>
            fetchJson('/sms/send', { method: 'POST', body: JSON.stringify(data) }),
        list: (params?: { clientId?: string; matterId?: string; limit?: number }) => {
            const query = new URLSearchParams();
            if (params?.clientId) query.append('clientId', params.clientId);
            if (params?.matterId) query.append('matterId', params.matterId);
            if (params?.limit) query.append('limit', params.limit.toString());
            return fetchJson(`/sms?${query.toString()}`);
        },
        conversation: (phoneNumber: string, limit?: number) =>
            fetchJson(`/sms/conversation/${encodeURIComponent(phoneNumber)}${limit ? `?limit=${limit}` : ''}`),
        // Templates
        templates: {
            list: (category?: string) => fetchJson(`/sms/templates${category ? `?category=${category}` : ''}`),
            create: (data: { name: string; body: string; category?: string; variables?: string }) =>
                fetchJson('/sms/templates', { method: 'POST', body: JSON.stringify(data) }),
            update: (id: string, data: any) => fetchJson(`/sms/templates/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
            delete: (id: string) => fetchJson(`/sms/templates/${id}`, { method: 'DELETE' }),
            seed: () => fetchJson('/sms/templates/seed', { method: 'POST' }),
        },
        // Reminders
        reminders: {
            list: (params?: { status?: string; days?: number }) => {
                const query = new URLSearchParams();
                if (params?.status) query.append('status', params.status);
                if (params?.days) query.append('days', params.days.toString());
                return fetchJson(`/sms/reminders?${query.toString()}`);
            },
            create: (data: { reminderType: string; toNumber: string; message: string; scheduledFor: string; entityId?: string; clientId?: string }) =>
                fetchJson('/sms/reminders', { method: 'POST', body: JSON.stringify(data) }),
            cancel: (id: string) => fetchJson(`/sms/reminders/${id}/cancel`, { method: 'POST' }),
            process: () => fetchJson('/sms/reminders/process', { method: 'POST' }),
        },
    },

    // Intake Forms
    intake: {
        // Forms
        forms: {
            list: (activeOnly?: boolean) => fetchJson(`/intake/forms${activeOnly !== undefined ? `?activeOnly=${activeOnly}` : ''}`),
            get: (id: string) => fetchJson(`/intake/forms/${id}`),
            create: (data: { name: string; description?: string; practiceArea?: string; fieldsJson?: string }) =>
                fetchJson('/intake/forms', { method: 'POST', body: JSON.stringify(data) }),
            update: (id: string, data: any) => fetchJson(`/intake/forms/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
            delete: (id: string) => fetchJson(`/intake/forms/${id}`, { method: 'DELETE' }),
        },
        // Public form
        public: {
            get: (slug: string) => fetchJson(`/intake/public/${slug}`),
            submit: (slug: string, dataJson: string) =>
                fetchJson(`/intake/public/${slug}/submit`, { method: 'POST', body: JSON.stringify({ dataJson }) }),
        },
        // Submissions
        submissions: {
            list: (params?: { formId?: string; status?: string; limit?: number }) => {
                const query = new URLSearchParams();
                if (params?.formId) query.append('formId', params.formId);
                if (params?.status) query.append('status', params.status);
                if (params?.limit) query.append('limit', params.limit.toString());
                return fetchJson(`/intake/submissions?${query.toString()}`);
            },
            get: (id: string) => fetchJson(`/intake/submissions/${id}`),
            review: (id: string, data: { status: string; notes?: string }) =>
                fetchJson(`/intake/submissions/${id}/review`, { method: 'POST', body: JSON.stringify(data) }),
            convertToLead: (id: string) => fetchJson(`/intake/submissions/${id}/convert-to-lead`, { method: 'POST' }),
            delete: (id: string) => fetchJson(`/intake/submissions/${id}`, { method: 'DELETE' }),
        },
    },

    // AI Innovation Suite
    ai: {
        // Legal Research
        research: {
            start: (data: { query: string; title?: string; matterId?: string; jurisdiction?: string; practiceArea?: string }) =>
                fetchJson('/ai/research', { method: 'POST', body: JSON.stringify(data) }),
            list: (params?: { matterId?: string; limit?: number }) => {
                const query = new URLSearchParams();
                if (params?.matterId) query.append('matterId', params.matterId);
                if (params?.limit) query.append('limit', params.limit.toString());
                return fetchJson(`/ai/research?${query.toString()}`);
            },
            get: (id: string) => fetchJson(`/ai/research/${id}`),
        },
        // Contract Analysis
        contracts: {
            analyze: (data: { documentId: string; documentContent: string; matterId?: string; contractType?: string }) =>
                fetchJson('/ai/analyze-contract', { method: 'POST', body: JSON.stringify(data) }),
            list: (params?: { documentId?: string; matterId?: string }) => {
                const query = new URLSearchParams();
                if (params?.documentId) query.append('documentId', params.documentId);
                if (params?.matterId) query.append('matterId', params.matterId);
                return fetchJson(`/ai/contract-analyses?${query.toString()}`);
            },
            get: (id: string) => fetchJson(`/ai/contract-analyses/${id}`),
        },
        // Case Prediction
        predictions: {
            predict: (data: { matterId: string; additionalContext?: string }) =>
                fetchJson('/ai/predict-case', { method: 'POST', body: JSON.stringify(data) }),
            list: (matterId: string) => fetchJson(`/ai/predictions/${matterId}`),
        },
    },

    // Staff Direct Messages
    staffMessages: {
        list: (userId?: string) =>
            fetchJson(`/staffmessages${userId ? `?userId=${encodeURIComponent(userId)}` : ''}`),
        thread: (userA: string, userB: string) =>
            fetchJson(`/staffmessages/thread?userA=${encodeURIComponent(userA)}&userB=${encodeURIComponent(userB)}`),
        send: (data: { senderId: string; recipientId: string; body: string }) =>
            fetchJson('/staffmessages', { method: 'POST', body: JSON.stringify(data) }),
        markRead: (id: string) =>
            fetchJson(`/staffmessages/${id}/read`, { method: 'POST' }),
    },
};
