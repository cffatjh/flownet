import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { Matter, Client, TimeEntry, Message, Expense, CalendarEvent, DocumentFile, Invoice, Lead, Task, TaskStatus, PracticeArea, FeeStructure, CaseStatus, Notification as AppNotification, TaskTemplate, ActiveTimer } from '../types';
import { api } from '../services/api';
import { useAuth } from './AuthContext';

// --- MOCK DATA FALLBACKS ---
const MOCK_CLIENTS: Client[] = [
  { id: 'c1', name: 'Stark Industries', email: 'tony@stark.com', phone: '212-555-0100', type: 'Corporate', status: 'Active', company: 'Stark Ind' },
  { id: 'c2', name: 'Wayne Enterprises', email: 'bruce@wayne.com', phone: '609-555-0100', type: 'Corporate', status: 'Active', company: 'Wayne Ent' },
  { id: 'c3', name: 'Diana Prince', email: 'diana@museum.org', phone: '202-555-0123', type: 'Individual', status: 'Active' }
];

const MOCK_MATTERS: Matter[] = [
  { id: 'm1', caseNumber: '24-050', name: 'Stark v. Rogers', client: MOCK_CLIENTS[0], practiceArea: PracticeArea.CivilLitigation, status: CaseStatus.Open, feeStructure: FeeStructure.Hourly, openDate: '2024-03-01', responsibleAttorney: 'HS', billableRate: 850, trustBalance: 250000 },
  { id: 'm2', caseNumber: '24-051', name: 'Wayne Merger', client: MOCK_CLIENTS[1], practiceArea: PracticeArea.Corporate, status: CaseStatus.Pending, feeStructure: FeeStructure.FlatFee, openDate: '2024-03-10', responsibleAttorney: 'JP', billableRate: 900, trustBalance: 100000 },
  { id: 'm3', caseNumber: '24-052', name: 'Prince Artifact Dispute', client: MOCK_CLIENTS[2], practiceArea: PracticeArea.IntellectualProperty, status: CaseStatus.Open, feeStructure: FeeStructure.Contingency, openDate: '2024-03-15', responsibleAttorney: 'MR', billableRate: 450, trustBalance: 5000 }
];

const MOCK_TASKS: Task[] = [
  { id: 't1', title: 'Draft Motion to Dismiss', dueDate: new Date(Date.now() + 86400000).toISOString(), priority: 'High', status: 'To Do', matterId: 'm1', assignedTo: 'MR' },
  { id: 't2', title: 'Review Merger Agreement', dueDate: new Date(Date.now() + 172800000).toISOString(), priority: 'Medium', status: 'In Progress', matterId: 'm2', assignedTo: 'JP' }
];

const MOCK_TIME_ENTRIES: TimeEntry[] = [
  { id: 'te1', matterId: 'm1', description: 'Client meeting regarding deposition strategy', duration: 90, rate: 850, date: new Date().toISOString(), billed: false, type: 'time' },
  { id: 'te2', matterId: 'm2', description: 'Due diligence review', duration: 120, rate: 900, date: new Date(Date.now() - 86400000).toISOString(), billed: true, type: 'time' }
];

const MOCK_EVENTS: CalendarEvent[] = [
  { id: 'ev1', title: 'Court Hearing: Stark v Rogers', date: new Date(Date.now() + 86400000 * 2).toISOString(), type: 'Court', matterId: 'm1' },
  { id: 'ev2', title: 'Partner Lunch', date: new Date(Date.now() + 86400000 * 3).toISOString(), type: 'Meeting' }
];

const MOCK_LEADS: Lead[] = [
  { id: 'l1', name: 'Lex Luthor', source: 'Referral', status: 'New', estimatedValue: 50000, practiceArea: PracticeArea.CriminalDefense }
];

const MOCK_INVOICES: Invoice[] = [
  {
    id: 'inv1',
    number: 'INV-2024-001',
    client: MOCK_CLIENTS[0],
    clientId: MOCK_CLIENTS[0].id,
    amount: 15400,
    dueDate: new Date(Date.now() + 86400000 * 10).toISOString(),
    status: 'SENT' as any,
    subtotal: 14000,
    taxAmount: 1400,
    discount: 0,
    amountPaid: 0,
    balance: 15400,
    issueDate: new Date().toISOString(),
    createdAt: new Date().toISOString()
  }
];

interface DataContextType {
  matters: Matter[];
  clients: Client[];
  timeEntries: TimeEntry[];
  expenses: Expense[];
  messages: Message[];
  events: CalendarEvent[];
  documents: DocumentFile[];
  invoices: Invoice[];
  leads: Lead[];
  tasks: Task[];
  taskTemplates: TaskTemplate[];
  notifications: AppNotification[];

  // Timer Actions
  activeTimer: ActiveTimer | null;
  startTimer: (matterId: string | undefined, description: string, rate?: number) => void;
  stopTimer: () => Promise<void>;
  updateTimer: (elapsed: number) => void;
  pauseTimer: () => void;
  resumeTimer: () => void;

  addMatter: (item: any) => Promise<void>;
  updateMatter: (id: string, data: Partial<Matter>) => Promise<void>;
  deleteMatter: (id: string) => Promise<void>;
  addTimeEntry: (item: any) => Promise<void>;
  addExpense: (item: any) => void;
  addMessage: (item: Message) => void;
  markMessageRead: (id: string) => void;
  addEvent: (item: any) => Promise<void>;
  deleteEvent: (id: string) => Promise<void>;
  addDocument: (item: DocumentFile) => void;
  updateDocument: (id: string, data: Partial<DocumentFile>) => void;
  deleteDocument: (id: string) => void;
  addInvoice: (item: any) => Promise<void>;
  updateInvoice: (id: string, data: any) => void;
  deleteInvoice: (id: string) => void;
  addClient: (item: any) => Promise<Client>;
  updateClient: (id: string, data: Partial<Client>) => Promise<void>;
  addLead: (item: any) => Promise<void>;
  updateLead: (id: string, data: Partial<Lead>) => Promise<void>;
  deleteLead: (id: string) => Promise<void>;
  addTask: (item: any) => Promise<void>;
  updateTaskStatus: (id: string, status: TaskStatus) => Promise<void>;
  updateTask: (id: string, data: Partial<Task>) => Promise<void>;
  deleteTask: (id: string) => Promise<void>;
  archiveTask: (id: string) => Promise<void>;
  createTasksFromTemplate: (data: { templateId: string; matterId?: string; assignedTo?: string; baseDate?: string }) => Promise<void>;
  markAsBilled: (matterId: string) => Promise<void>;
  markNotificationRead: (id: string) => Promise<void>;
  markNotificationUnread: (id: string) => Promise<void>;
  markAllNotificationsRead: () => Promise<void>;
  updateUserProfile: (data: any) => Promise<void>;
  bulkAssignDocuments: (ids: string[], matterId?: string | null) => Promise<void>;
}

const DataContext = createContext<DataContextType | undefined>(undefined);

export const DataProvider = ({ children }: { children: ReactNode }) => {
  const { isAuthenticated, user } = useAuth();

  // --- STATE ---
  const [activeTimer, setActiveTimer] = useState<ActiveTimer | null>(() => {
    // Load from local storage on init
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('jf_active_timer');
      return saved ? JSON.parse(saved) : null;
    }
    return null;
  });

  const [matters, setMatters] = useState<Matter[]>([]);
  const [clients, setClients] = useState<Client[]>([]);
  const [timeEntries, setTimeEntries] = useState<TimeEntry[]>([]);
  const [tasks, setTasks] = useState<Task[]>([]);
  const [taskTemplates, setTaskTemplates] = useState<TaskTemplate[]>([]);
  const [events, setEvents] = useState<CalendarEvent[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [leads, setLeads] = useState<Lead[]>([]);
  const [notifications, setNotifications] = useState<AppNotification[]>([]);

  // Local-only state
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [messages, setMessages] = useState<Message[]>([
    { id: 'msg1', from: 'Jessica Pearson', subject: 'Managing Partner Meeting', preview: 'We need to discuss the new associates...', date: '09:00 AM', read: false }
  ]);
  const [documents, setDocuments] = useState<DocumentFile[]>([]);

  // Timer Persistence
  useEffect(() => {
    if (activeTimer) {
      localStorage.setItem('jf_active_timer', JSON.stringify(activeTimer));
    } else {
      localStorage.removeItem('jf_active_timer');
    }
  }, [activeTimer]);

  const startTimer = (matterId: string | undefined, description: string, rate?: number) => {
    const newTimer: ActiveTimer = {
      startTime: Date.now(),
      matterId,
      description,
      isRunning: true,
      elapsed: 0,
      rate
    };
    setActiveTimer(newTimer);
  };

  const pauseTimer = () => {
    if (activeTimer && activeTimer.isRunning) {
      const now = Date.now();
      const additional = now - activeTimer.startTime;
      setActiveTimer({
        ...activeTimer,
        isRunning: false,
        elapsed: activeTimer.elapsed + additional
      });
    }
  };

  const resumeTimer = () => {
    if (activeTimer && !activeTimer.isRunning) {
      setActiveTimer({
        ...activeTimer,
        isRunning: true,
        startTime: Date.now()
      });
    }
  };

  const updateTimer = (elapsed: number) => {
    // This is mostly for UI sync if needed, but the real truth is calculated
  };

  const stopTimer = async () => {
    if (!activeTimer) return;

    // Calculate total duration
    let totalDurationMs = activeTimer.elapsed;
    if (activeTimer.isRunning) {
      totalDurationMs += (Date.now() - activeTimer.startTime);
    }

    // Convert to minutes (minimum 1 minute?)
    const minutes = Math.ceil(totalDurationMs / 1000 / 60);

    // Create Time Entry
    await addTimeEntry({
      matterId: activeTimer.matterId,
      description: activeTimer.description,
      duration: minutes,
      date: new Date().toISOString(),
      rate: activeTimer.rate || 0, // Use stored rate or default to 0
      billed: false,
      type: 'time'
    });

    setActiveTimer(null);
  };

  const parseDocTags = (raw: any): string[] | undefined => {
    if (!raw) return undefined;
    if (Array.isArray(raw)) return raw.map(String);
    if (typeof raw === 'string') {
      try {
        const parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) return parsed.map(String);
      } catch {
        // allow comma-separated
        return raw.split(',').map(s => s.trim()).filter(Boolean);
      }
    }
    return undefined;
  };

  const normalizeDocument = (d: any): DocumentFile => {
    const mime = (d.mimeType || '').toLowerCase();
    const type: DocumentFile['type'] =
      mime.includes('pdf') ? 'pdf' :
        mime.includes('word') ? 'docx' :
          mime.includes('text') ? 'txt' :
            mime.includes('image') ? 'img' : 'txt';

    const sizeStr = typeof d.fileSize === 'number'
      ? `${(d.fileSize / 1024 / 1024).toFixed(2)} MB`
      : undefined;

    return {
      id: d.id,
      name: d.name,
      type,
      size: sizeStr,
      fileSize: d.fileSize,
      updatedAt: d.updatedAt || d.createdAt || new Date().toISOString(),
      matterId: d.matterId || undefined,
      filePath: d.filePath || undefined,
      description: d.description || undefined,
      tags: parseDocTags(d.tags),
      version: d.version || undefined,
    };
  };

  // --- INITIAL LOAD ---
  useEffect(() => {
    const loadData = async () => {
      try {
        // Check if user is authenticated before making API calls
        const token = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null;

        // Only make API calls if user is authenticated
        if (!token || !isAuthenticated) {
          // Use mock data if not authenticated
          console.log('🔄 Not authenticated, using mock data');
          setMatters(MOCK_MATTERS);
          setClients(MOCK_CLIENTS);
          setTasks(MOCK_TASKS);
          setTimeEntries(MOCK_TIME_ENTRIES);
          setEvents(MOCK_EVENTS);
          setLeads(MOCK_LEADS);
          setInvoices(MOCK_INVOICES);
          return;
        }

        console.log('🔄 Fetching data from API...');
        const [m, t, te, ex, c, l, e, i, n, docs, templates] = await Promise.all([
          api.getMatters().catch(() => null),
          api.getTasks().catch(() => null),
          api.getTimeEntries().catch(() => null),
          api.getExpenses().catch(() => null),
          api.getClients().catch(() => null),
          api.getLeads().catch(() => null),
          api.getEvents().catch(() => null),
          api.getInvoices().catch(() => null),
          api.getNotifications(user?.id).catch(() => []),
          api.getDocuments().catch(() => []),
          api.getTaskTemplates().catch(() => [])
        ]);

        // IMPORTANT: Only update state if API returned valid data (not null)
        // If API returns null/undefined, keep using mock/previous data
        if (Array.isArray(m)) setMatters(m);
        else console.warn('Matters API returned null, keeping current data');

        if (Array.isArray(t)) setTasks(t);
        else {
          console.warn('Tasks API returned null, using mock data');
          setTasks(MOCK_TASKS);
        }

        if (Array.isArray(te)) setTimeEntries(te);
        else setTimeEntries(MOCK_TIME_ENTRIES);

        if (Array.isArray(ex)) setExpenses(ex);
        else setExpenses([]);

        if (Array.isArray(c)) setClients(c);
        else setClients(MOCK_CLIENTS);

        if (Array.isArray(l)) setLeads(l);
        else setLeads(MOCK_LEADS);

        if (Array.isArray(e)) setEvents(e);
        else setEvents(MOCK_EVENTS);

        if (Array.isArray(i)) setInvoices(i);
        else setInvoices(MOCK_INVOICES);

        setNotifications(n || []);
        setDocuments((docs || []).map(normalizeDocument));
        setTaskTemplates(templates || []);
        console.log('✅ Data loaded successfully from API');
      } catch (error) {
        console.warn('⚠️ Failed to load data from backend. Falling back to Mock Data.', error);
        // Fallback to MOCK DATA so app is usable
        setMatters(MOCK_MATTERS);
        setClients(MOCK_CLIENTS);
        setTasks(MOCK_TASKS);
        setTimeEntries(MOCK_TIME_ENTRIES);
        setEvents(MOCK_EVENTS);
        setLeads(MOCK_LEADS);
        setInvoices(MOCK_INVOICES);
      }
    };
    loadData();
  }, [isAuthenticated]);

  // --- NOTIFICATION POLLING ---
  useEffect(() => {
    if (!isAuthenticated) return;

    const fetchNotifications = async () => {
      try {
        const notifs = await api.getNotifications(user?.id);
        if (notifs) setNotifications(notifs);
      } catch (e) {
        // console.error("Notification Poll Error", e); // Silent fail
      }
    };

    // Initial fetch
    fetchNotifications();

    // Poll every 60 seconds
    const interval = setInterval(fetchNotifications, 60000);
    return () => clearInterval(interval);
  }, [isAuthenticated]);

  // --- EVENT REMINDER SYSTEM ---
  useEffect(() => {
    if (!isAuthenticated) return;

    // Request browser notification permission
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission().then(permission => {
        console.log('Notification permission:', permission);
      });
    }

    const checkEventReminders = () => {
      const now = Date.now();

      events.forEach(event => {
        if (!event.reminderMinutes || event.reminderMinutes === 0) return;
        if (event.reminderSent) return;

        const eventTime = new Date(event.date).getTime();
        const reminderTime = eventTime - (event.reminderMinutes * 60 * 1000);

        // Check if we're within 1 minute of the reminder time
        if (now >= reminderTime && now < reminderTime + 60000) {
          // Create notification
          const minutesUntil = Math.round((eventTime - now) / 60000);
          const timeStr = minutesUntil > 60
            ? `${Math.round(minutesUntil / 60)} hour(s)`
            : `${minutesUntil} minute(s)`;

          const newNotification: AppNotification = {
            id: `notif-event-${event.id}-${Date.now()}`,
            userId: 'current',
            title: `📅 Upcoming Event`,
            message: `"${event.title}" starts in ${timeStr}!`,
            type: 'warning',
            read: false,
            link: 'tab:calendar',
            createdAt: new Date().toISOString()
          };

          setNotifications(prev => [newNotification, ...prev]);

          // Mark reminder as sent in local state
          setEvents(prev => prev.map(e =>
            e.id === event.id ? { ...e, reminderSent: true } : e
          ));

          // Show browser notification if permission granted
          if ('Notification' in window && Notification.permission === 'granted') {
            new window.Notification(`📅 ${event.title}`, {
              body: `Event starts in ${timeStr}!`,
              icon: '/icons/icon-192.png',
              tag: `event-${event.id}`
            });
          }

          // Also play a sound if available
          try {
            const audio = new Audio('/notification.mp3');
            audio.volume = 0.5;
            audio.play().catch(() => { }); // Ignore errors if audio can't play
          } catch (e) {
            // Audio not available
          }
        }
      });
    };

    // Check immediately and then every 30 seconds
    checkEventReminders();
    const interval = setInterval(checkEventReminders, 30000);

    return () => clearInterval(interval);
  }, [events, isAuthenticated]);

  // --- ACTIONS (Optimistic Updates) ---

  const addMatter = async (matterData: any) => {
    // Optimistic
    const tempId = `m-temp-${Date.now()}`;
    const optimisticClient = matterData.client || {
      id: matterData.clientId || `c-temp-${Date.now()}`,
      name: matterData.clientName || 'Client',
      email: matterData.clientEmail || '',
      phone: matterData.clientPhone || '',
      type: 'Individual',
      status: 'Active'
    };
    const optimisticMatter = { ...matterData, id: tempId, client: optimisticClient };
    setMatters(prev => [optimisticMatter, ...prev]);

    try {
      const newMatter = await api.createMatter(matterData);
      // Replace temp with real
      const hydratedMatter = { ...newMatter, client: newMatter.client || optimisticClient };
      setMatters(prev => [hydratedMatter, ...prev.filter(m => m.id !== tempId)]);
      const freshClients = await api.getClients();
      setClients(freshClients);
    } catch (e) {
      console.error("API Error (addMatter) - operating offline", e);
    }
  };

  const updateMatter = async (id: string, data: Partial<Matter>) => {
    // Optimistic update
    setMatters(prev => prev.map(m => m.id === id ? { ...m, ...data, client: data.client || m.client } : m));
    try {
      const updated = await api.updateMatter(id, data);
      setMatters(prev => prev.map(m => m.id === id ? { ...m, ...updated } : m));
    } catch (e) {
      console.error("API Error (updateMatter)", e);
    }
  };

  const deleteMatter = async (id: string) => {
    const prev = matters;
    setMatters(prev => prev.filter(m => m.id !== id));
    try {
      await api.deleteMatter(id);
    } catch (e) {
      console.error("API Error (deleteMatter)", e);
      setMatters(prev); // revert
    }
  };

  const addTask = async (taskData: any) => {
    const tempId = `t-${Date.now()}`;
    const tempTask = { ...taskData, id: tempId, status: taskData.status || 'To Do', priority: taskData.priority || 'Medium' };

    console.log('[addTask] Creating task with temp ID:', tempId);
    setTasks(prev => [...prev, tempTask]);

    try {
      const newTask = await api.createTask(taskData);
      if (newTask && newTask.id) {
        console.log('[addTask] Task created successfully with real ID:', newTask.id);
        // Replace temp task with real one from server
        setTasks(prev => prev.map(t => t.id === tempId ? { ...t, ...newTask, id: newTask.id } : t));
      } else {
        console.warn('[addTask] API returned null, removing temp task');
        setTasks(prev => prev.filter(t => t.id !== tempId));
      }
    } catch (e) {
      console.error('[addTask] API Error - removing temp task:', e);
      // Remove temp task on error
      setTasks(prev => prev.filter(t => t.id !== tempId));
    }
  };

  const updateTaskStatus = async (id: string, status: TaskStatus) => {
    setTasks(prev => prev.map(t => t.id === id ? { ...t, status } : t));
    try { await api.updateTaskStatus(id, status); } catch (e) { console.error("API Error", e); }
  };

  const updateTask = async (id: string, data: Partial<Task>) => {
    setTasks(prev => prev.map(t => t.id === id ? { ...t, ...data } : t));
    try {
      const updated = await api.updateTask(id, data);
      setTasks(prev => prev.map(t => t.id === id ? { ...t, ...updated } : t));
    } catch (e) {
      console.error("API Error (updateTask)", e);
    }
  };

  const createTasksFromTemplate = async (data: { templateId: string; matterId?: string; assignedTo?: string; baseDate?: string }) => {
    try {
      const res = await api.createTasksFromTemplate(data);
      if (res?.tasks) {
        setTasks(prev => [...res.tasks, ...prev]);
      }
    } catch (e) {
      console.error("API Error (createTasksFromTemplate)", e);
    }
  };

  const deleteTask = async (id: string) => {
    console.log(`[deleteTask] Starting deletion of task: ${id}`);

    // Check if this is a temp task (never saved to server)
    const isTempTask = id.startsWith('t-');

    // Store previous state for potential rollback
    const prevTasks = [...tasks];

    // Optimistically remove from UI immediately
    setTasks(prev => {
      const filtered = prev.filter(t => t.id !== id);
      console.log(`[deleteTask] Optimistically removed. Prev count: ${prev.length}, New count: ${filtered.length}`);
      return filtered;
    });

    // If it's a temp task that was never saved, we're done
    if (isTempTask) {
      console.log('[deleteTask] Temp task removed (never persisted)');
      return;
    }

    try {
      // Call API to delete
      const result = await api.deleteTask(id);
      // Success! API returned (null for 204 is fine)
      console.log(`[deleteTask] API call successful. Result:`, result);
      // DO NOT revert - deletion was successful
    } catch (e: any) {
      // Only revert if there was an actual error
      console.error('[deleteTask] API Error - reverting:', e);
      console.error('[deleteTask] Error message:', e?.message || 'Unknown error');
      setTasks(prevTasks);
    }
  };

  const archiveTask = async (id: string) => {
    setTasks(prev => prev.map(t => t.id === id ? { ...t, status: 'Archived' as TaskStatus } : t));
    try {
      await api.updateTaskStatus(id, 'Archived');
    } catch (e) {
      console.error("API Error (archiveTask)", e);
    }
  };

  const addTimeEntry = async (entryData: any) => {
    const tempEntry = { ...entryData, id: `te-${Date.now()}` };
    setTimeEntries(prev => [tempEntry, ...prev]);
    try {
      const newEntry = await api.createTimeEntry(entryData);
      setTimeEntries(prev => [newEntry, ...prev.filter(t => t.id !== tempEntry.id)]);
    } catch (e) { console.error("API Error", e); }
  };

  const addClient = async (clientData: any): Promise<Client> => {
    const temp = { ...clientData, id: `c-${Date.now()}` };
    setClients(prev => [temp, ...prev]);
    try {
      const newClient = await api.createClient(clientData);
      setClients(prev => [newClient, ...prev.filter(c => c.id !== temp.id)]);
      return newClient;
    } catch (e) {
      console.error("API Error", e);
      setClients(prev => prev.filter(c => c.id !== temp.id));
      throw e;
    }
  };

  const updateClient = async (id: string, data: Partial<Client>) => {
    const prev = clients;
    // Optimistic update
    setClients(prevClients => prevClients.map(c => c.id === id ? { ...c, ...data } : c));
    try {
      const updated = await api.admin.updateClient(id, data);
      if (updated) {
        setClients(prevClients => prevClients.map(c => c.id === id ? { ...c, ...updated } : c));
      }
    } catch (e) {
      console.error("API Error (updateClient)", e);
      setClients(prev); // revert
      throw e;
    }
  };

  const addLead = async (leadData: any) => {
    const temp = { ...leadData, id: `l-${Date.now()}` };
    setLeads(prev => [temp, ...prev]);
    try {
      const newLead = await api.createLead(leadData);
      setLeads(prev => [newLead, ...prev.filter(l => l.id !== temp.id)]);
    } catch (e) { console.error("API Error", e); }
  };

  const updateLead = async (id: string, data: Partial<Lead>) => {
    setLeads(prev => prev.map(l => l.id === id ? { ...l, ...data } : l));
    try {
      const updated = await api.updateLead(id, data);
      setLeads(prev => prev.map(l => l.id === id ? updated : l));
    } catch (e) { console.error("API Error (updateLead)", e); }
  };

  const deleteLead = async (id: string) => {
    const prev = leads;
    setLeads(prev => prev.filter(l => l.id !== id));
    try {
      await api.deleteLead(id);
    } catch (e) {
      console.error("API Error (deleteLead)", e);
      setLeads(prev);
    }
  };

  const addEvent = async (eventData: any) => {
    const temp = { ...eventData, id: `ev-${Date.now()}` };
    setEvents(prev => [...prev, temp].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()));
    try {
      const newEvent = await api.createEvent(eventData);
      setEvents(prev => [...prev.filter(e => e.id !== temp.id), newEvent].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()));
    } catch (e) { console.error("API Error", e); }
  };

  const deleteEvent = async (id: string) => {
    const prev = events;
    setEvents(prev => prev.filter(e => e.id !== id));
    try {
      await api.deleteEvent(id);
    } catch (e) {
      console.error("API Error (deleteEvent)", e);
      setEvents(prev);
    }
  };

  const addInvoice = async (invoiceData: any) => {
    const temp = { ...invoiceData, id: `inv-${Date.now()}` };
    setInvoices(prev => [temp, ...prev]);
    try {
      const newInvoice = await api.createInvoice(invoiceData);
      setInvoices(prev => [newInvoice, ...prev.filter(i => i.id !== temp.id)]);
    } catch (e) { console.error("API Error", e); }
  };

  const updateInvoice = (id: string, data: any) => {
    setInvoices(prev => prev.map(inv => inv.id === id ? { ...inv, ...data } : inv));
  };

  const deleteInvoice = (id: string) => {
    setInvoices(prev => prev.filter(inv => inv.id !== id));
  };

  const markAsBilled = async (matterId: string) => {
    // Optimistic
    setTimeEntries(prev => prev.map(e => e.matterId === matterId ? { ...e, billed: true } : e));
    setExpenses(prev => prev.map(e => e.matterId === matterId ? { ...e, billed: true } : e));

    try {
      await api.markAsBilled(matterId);
    } catch (e) { console.error("API Error", e); }
  };

  // Local Actions
  const addExpense = async (expenseData: any) => {
    const tempExpense = { ...expenseData, id: `e-${Date.now()}` };
    setExpenses(prev => [tempExpense, ...prev]);
    try {
      const newExpense = await api.createExpense(expenseData);
      setExpenses(prev => [newExpense, ...prev.filter(e => e.id !== tempExpense.id)]);
    } catch (e) {
      console.error("API Error (addExpense)", e);
    }
  };
  const addMessage = (msg: Message) => setMessages(prev => [msg, ...prev]);
  const markMessageRead = (id: string) => setMessages(prev => prev.map(m => m.id === id ? { ...m, read: true } : m));
  const addDocument = (doc: DocumentFile) => setDocuments(prev => [doc, ...prev]);
  const updateDocument = async (id: string, data: Partial<DocumentFile>) => {
    // optimistic
    setDocuments(prev => prev.map(doc => doc.id === id ? { ...doc, ...data } : doc));
    try {
      const payload: any = {};
      if ('matterId' in data) payload.matterId = data.matterId ?? null;
      if ('description' in data) payload.description = data.description ?? null;
      if ('tags' in data) payload.tags = data.tags ?? null;
      const updated = await api.updateDocument(id, payload);
      setDocuments(prev => prev.map(doc => doc.id === id ? { ...doc, ...normalizeDocument(updated) } : doc));
    } catch (e) {
      console.error("API Error (updateDocument)", e);
    }
  };
  const bulkAssignDocuments = async (ids: string[], matterId?: string | null) => {
    // optimistic
    setDocuments(prev => prev.map(d => ids.includes(d.id) ? { ...d, matterId: matterId || undefined } : d));
    try {
      await api.bulkAssignDocuments({ ids, matterId: matterId || null });
    } catch (e) {
      console.error("API Error (bulkAssignDocuments)", e);
    }
  };
  const deleteDocument = (id: string) => {
    setDocuments(prev => prev.filter(doc => doc.id !== id));
  };

  const markNotificationRead = async (id: string) => {
    setNotifications(prev => prev.map(n => n.id === id ? { ...n, read: true } : n));
    try {
      await api.markNotificationRead(id);
    } catch (e) { console.error("API Error", e); }
  };

  const markNotificationUnread = async (id: string) => {
    setNotifications(prev => prev.map(n => n.id === id ? { ...n, read: false } : n));
    try {
      await api.markNotificationUnread(id);
    } catch (e) { console.error("API Error", e); }
  };

  const markAllNotificationsRead = async () => {
    setNotifications(prev => prev.map(n => ({ ...n, read: true })));
    try {
      await api.markAllNotificationsRead();
    } catch (e) { console.error("API Error", e); }
  };

  const updateUserProfile = async (data: any) => {
    try {
      await api.updateUserProfile(data);
    } catch (e) {
      console.error("API Error", e);
      throw e;
    }
  };

  return (
    <DataContext.Provider value={{
      matters, clients, timeEntries, expenses, messages, events, documents, invoices, leads, tasks, taskTemplates, notifications,
      activeTimer, startTimer, stopTimer, updateTimer, pauseTimer, resumeTimer,
      addMatter, updateMatter, deleteMatter,
      addTimeEntry, addExpense, addMessage, markMessageRead, addEvent, deleteEvent, addDocument, updateDocument, deleteDocument, addInvoice, updateInvoice, deleteInvoice, addClient, addLead, updateLead, deleteLead, addTask,
      updateTaskStatus, updateTask, deleteTask, archiveTask, createTasksFromTemplate, markAsBilled, markNotificationRead, markNotificationUnread, markAllNotificationsRead, updateUserProfile, updateClient,
      bulkAssignDocuments
    }}>
      {children}
    </DataContext.Provider>
  );
};

export const useData = () => {
  const context = useContext(DataContext);
  if (!context) {
    throw new Error('useData must be used within a DataProvider');
  }
  return context;
};
