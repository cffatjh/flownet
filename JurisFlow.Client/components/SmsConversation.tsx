'use client';

import { useState, useEffect } from 'react';
import { MessageSquare, Send, Phone, Clock, User, ChevronRight, Plus, Search } from './Icons';
import { api } from '../services/api';

interface SmsMessage {
    id: string;
    fromNumber: string;
    toNumber: string;
    body: string;
    direction: string;
    status: string;
    clientId?: string;
    sentAt?: string;
    createdAt: string;
}

interface SmsTemplate {
    id: string;
    name: string;
    body: string;
    category: string;
}

interface Client {
    id: string;
    name: string;
    phone?: string;
}

interface SmsConversationProps {
    clientId?: string;
    matterId?: string;
    phoneNumber?: string;
    onClose?: () => void;
}

export default function SmsConversation({ clientId, matterId, phoneNumber, onClose }: SmsConversationProps) {
    const [messages, setMessages] = useState<SmsMessage[]>([]);
    const [templates, setTemplates] = useState<SmsTemplate[]>([]);
    const [clients, setClients] = useState<Client[]>([]);
    const [loading, setLoading] = useState(true);
    const [sending, setSending] = useState(false);

    const [selectedClient, setSelectedClient] = useState<Client | null>(null);
    const [toNumber, setToNumber] = useState(phoneNumber || '');
    const [messageBody, setMessageBody] = useState('');
    const [showTemplates, setShowTemplates] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');

    useEffect(() => {
        loadData();
    }, [clientId, matterId, phoneNumber]);

    const loadData = async () => {
        setLoading(true);
        try {
            const [messagesData, templatesData, clientsData] = await Promise.all([
                phoneNumber
                    ? api.sms.conversation(phoneNumber)
                    : api.sms.list({ clientId, matterId }),
                api.sms.templates.list(),
                api.getClients()
            ]);

            setMessages(messagesData);
            setTemplates(templatesData);
            setClients(clientsData);

            if (clientId) {
                const client = clientsData.find((c: Client) => c.id === clientId);
                if (client) {
                    setSelectedClient(client);
                    setToNumber(client.phone || '');
                }
            }
        } catch (error) {
            console.error('Failed to load SMS data:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleSend = async () => {
        if (!toNumber || !messageBody.trim()) return;

        setSending(true);
        try {
            await api.sms.send({
                toNumber,
                body: messageBody,
                clientId: selectedClient?.id,
                matterId
            });

            setMessageBody('');
            await loadData();
        } catch (error) {
            console.error('Failed to send SMS:', error);
        } finally {
            setSending(false);
        }
    };

    const handleTemplateSelect = (template: SmsTemplate) => {
        setMessageBody(template.body);
        setShowTemplates(false);
    };

    const handleClientSelect = (client: Client) => {
        setSelectedClient(client);
        setToNumber(client.phone || '');
        setSearchQuery('');
    };

    const formatTime = (dateString: string) => {
        return new Date(dateString).toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            hour: 'numeric',
            minute: '2-digit'
        });
    };

    const filteredClients = clients.filter(c =>
        c.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        c.phone?.includes(searchQuery)
    );

    if (loading) {
        return (
            <div className="flex items-center justify-center h-64">
                <div className="w-8 h-8 border-2 border-blue-200 border-t-blue-600 rounded-full animate-spin" />
            </div>
        );
    }

    return (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden flex flex-col h-[600px]">
            {/* Header */}
            <div className="px-4 py-3 border-b bg-gradient-to-r from-green-50 to-emerald-50 flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-full bg-green-100 flex items-center justify-center">
                        <MessageSquare className="w-5 h-5 text-green-600" />
                    </div>
                    <div>
                        <h2 className="font-semibold text-slate-800">SMS Messages</h2>
                        {selectedClient && (
                            <p className="text-sm text-slate-500">{selectedClient.name} • {toNumber}</p>
                        )}
                    </div>
                </div>
                {onClose && (
                    <button onClick={onClose} className="text-slate-400 hover:text-slate-600">×</button>
                )}
            </div>

            {/* Client Selector (if no client selected) */}
            {!selectedClient && !phoneNumber && (
                <div className="p-4 border-b">
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                        <input
                            type="text"
                            placeholder="Search clients..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="w-full pl-10 pr-4 py-2 border border-slate-200 rounded-lg"
                        />
                    </div>
                    {searchQuery && (
                        <div className="mt-2 max-h-40 overflow-auto border border-slate-200 rounded-lg">
                            {filteredClients.map(client => (
                                <button
                                    key={client.id}
                                    onClick={() => handleClientSelect(client)}
                                    className="w-full px-4 py-2 text-left hover:bg-slate-50 flex items-center justify-between"
                                >
                                    <span>{client.name}</span>
                                    <span className="text-sm text-slate-500">{client.phone || 'No phone'}</span>
                                </button>
                            ))}
                        </div>
                    )}
                </div>
            )}

            {/* Messages */}
            <div className="flex-1 overflow-auto p-4 space-y-3 bg-slate-50">
                {messages.length === 0 ? (
                    <div className="text-center text-slate-500 py-12">
                        <MessageSquare className="w-12 h-12 mx-auto mb-3 opacity-30" />
                        <p>No messages yet</p>
                        <p className="text-sm">Send a message to start the conversation</p>
                    </div>
                ) : (
                    messages.map(msg => (
                        <div
                            key={msg.id}
                            className={`flex ${msg.direction === 'Outbound' ? 'justify-end' : 'justify-start'}`}
                        >
                            <div
                                className={`max-w-[75%] rounded-2xl px-4 py-2 ${msg.direction === 'Outbound'
                                        ? 'bg-green-600 text-white rounded-br-sm'
                                        : 'bg-white text-slate-800 border border-slate-200 rounded-bl-sm'
                                    }`}
                            >
                                <p className="text-sm">{msg.body}</p>
                                <div className={`text-xs mt-1 flex items-center gap-1 ${msg.direction === 'Outbound' ? 'text-green-200' : 'text-slate-400'
                                    }`}>
                                    <Clock className="w-3 h-3" />
                                    {formatTime(msg.sentAt || msg.createdAt)}
                                    {msg.direction === 'Outbound' && (
                                        <span className="ml-1">
                                            {msg.status === 'Delivered' ? '✓✓' : msg.status === 'Sent' ? '✓' : ''}
                                        </span>
                                    )}
                                </div>
                            </div>
                        </div>
                    ))
                )}
            </div>

            {/* Templates Panel */}
            {showTemplates && (
                <div className="border-t p-3 bg-white max-h-40 overflow-auto">
                    <p className="text-xs font-medium text-slate-500 mb-2">Quick Templates</p>
                    <div className="space-y-1">
                        {templates.map(template => (
                            <button
                                key={template.id}
                                onClick={() => handleTemplateSelect(template)}
                                className="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-100 text-sm"
                            >
                                <span className="font-medium text-slate-700">{template.name}</span>
                                <p className="text-xs text-slate-500 truncate">{template.body}</p>
                            </button>
                        ))}
                    </div>
                </div>
            )}

            {/* Compose */}
            <div className="border-t p-3 bg-white">
                {!phoneNumber && !selectedClient && (
                    <div className="mb-2">
                        <input
                            type="tel"
                            placeholder="Phone number (+1...)"
                            value={toNumber}
                            onChange={(e) => setToNumber(e.target.value)}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm"
                        />
                    </div>
                )}
                <div className="flex gap-2">
                    <button
                        onClick={() => setShowTemplates(!showTemplates)}
                        className="px-3 py-2 border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-600"
                        title="Templates"
                    >
                        <Plus className="w-5 h-5" />
                    </button>
                    <input
                        type="text"
                        placeholder="Type a message..."
                        value={messageBody}
                        onChange={(e) => setMessageBody(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && handleSend()}
                        className="flex-1 px-4 py-2 border border-slate-200 rounded-lg"
                    />
                    <button
                        onClick={handleSend}
                        disabled={sending || !toNumber || !messageBody.trim()}
                        className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 transition flex items-center gap-2"
                    >
                        {sending ? (
                            <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                        ) : (
                            <Send className="w-5 h-5" />
                        )}
                    </button>
                </div>
            </div>
        </div>
    );
}
