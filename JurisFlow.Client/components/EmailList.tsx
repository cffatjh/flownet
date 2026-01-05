'use client';

import { useState, useEffect } from 'react';
import { Mail, Paperclip, Link2, ChevronRight, Search, Filter, RefreshCw, ExternalLink } from './Icons';
import { api } from '../services/api';

interface EmailMessage {
    id: string;
    subject: string;
    fromAddress: string;
    fromName: string;
    toAddresses: string;
    folder: string;
    isRead: boolean;
    hasAttachments: boolean;
    importance: string;
    receivedAt: string;
    matterId?: string;
    clientId?: string;
    bodyText?: string;
    bodyHtml?: string;
}

interface EmailListProps {
    matterId?: string;
    clientId?: string;
    onSelectEmail?: (email: EmailMessage) => void;
}

export default function EmailList({ matterId, clientId, onSelectEmail }: EmailListProps) {
    const [emails, setEmails] = useState<EmailMessage[]>([]);
    const [selectedEmail, setSelectedEmail] = useState<EmailMessage | null>(null);
    const [loading, setLoading] = useState(true);
    const [syncing, setSyncing] = useState(false);

    const [folder, setFolder] = useState<string>('');
    const [searchQuery, setSearchQuery] = useState('');
    const [showLinkModal, setShowLinkModal] = useState(false);
    const [linkingEmail, setLinkingEmail] = useState<EmailMessage | null>(null);

    useEffect(() => {
        loadEmails();
    }, [matterId, clientId, folder]);

    const loadEmails = async () => {
        setLoading(true);
        try {
            const data = await api.emails.list({ matterId, clientId, folder: folder || undefined });
            setEmails(data);
        } catch (error) {
            console.error('Failed to load emails:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleSelectEmail = async (email: EmailMessage) => {
        try {
            const fullEmail = await api.emails.get(email.id);
            setSelectedEmail(fullEmail);
            onSelectEmail?.(fullEmail);

            // Mark as read in list
            setEmails(prev => prev.map(e =>
                e.id === email.id ? { ...e, isRead: true } : e
            ));
        } catch (error) {
            console.error('Failed to load email:', error);
        }
    };

    const handleSync = async () => {
        setSyncing(true);
        try {
            const accounts = await api.emails.accounts.list();
            if (accounts.length > 0) {
                await api.emails.accounts.sync(accounts[0].id);
            }
            await loadEmails();
        } catch (error) {
            console.error('Failed to sync:', error);
        } finally {
            setSyncing(false);
        }
    };

    const handleLinkEmail = async (targetMatterId?: string, targetClientId?: string) => {
        if (!linkingEmail) return;

        try {
            await api.emails.link(linkingEmail.id, {
                matterId: targetMatterId,
                clientId: targetClientId
            });
            setShowLinkModal(false);
            setLinkingEmail(null);
            await loadEmails();
        } catch (error) {
            console.error('Failed to link email:', error);
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        const today = new Date();
        const isToday = date.toDateString() === today.toDateString();

        if (isToday) {
            return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
        }
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    };

    const filteredEmails = emails.filter(email =>
        email.subject.toLowerCase().includes(searchQuery.toLowerCase()) ||
        email.fromName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        email.fromAddress.toLowerCase().includes(searchQuery.toLowerCase())
    );

    return (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden flex h-[600px]">
            {/* Email List */}
            <div className="w-1/3 border-r flex flex-col">
                {/* Header */}
                <div className="px-4 py-3 border-b bg-gradient-to-r from-blue-50 to-indigo-50">
                    <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                            <Mail className="w-5 h-5 text-blue-600" />
                            <h2 className="font-semibold text-slate-800">Emails</h2>
                        </div>
                        <button
                            onClick={handleSync}
                            disabled={syncing}
                            className="p-2 hover:bg-white/50 rounded-lg transition"
                            title="Sync emails"
                        >
                            <RefreshCw className={`w-4 h-4 ${syncing ? 'animate-spin' : ''}`} />
                        </button>
                    </div>
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                        <input
                            type="text"
                            placeholder="Search emails..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="w-full pl-9 pr-3 py-1.5 border border-slate-200 rounded-lg text-sm"
                        />
                    </div>
                </div>

                {/* Folder Tabs */}
                <div className="flex border-b text-sm">
                    {['', 'Inbox', 'Sent'].map(f => (
                        <button
                            key={f}
                            onClick={() => setFolder(f)}
                            className={`flex-1 py-2 ${folder === f
                                    ? 'text-blue-600 border-b-2 border-blue-600'
                                    : 'text-slate-500 hover:text-slate-700'
                                }`}
                        >
                            {f || 'All'}
                        </button>
                    ))}
                </div>

                {/* Email List */}
                <div className="flex-1 overflow-auto">
                    {loading ? (
                        <div className="flex items-center justify-center h-32">
                            <div className="w-6 h-6 border-2 border-blue-200 border-t-blue-600 rounded-full animate-spin" />
                        </div>
                    ) : filteredEmails.length === 0 ? (
                        <div className="text-center text-slate-500 py-12">
                            <Mail className="w-10 h-10 mx-auto mb-2 opacity-30" />
                            <p className="text-sm">No emails found</p>
                        </div>
                    ) : (
                        filteredEmails.map(email => (
                            <button
                                key={email.id}
                                onClick={() => handleSelectEmail(email)}
                                className={`w-full text-left px-4 py-3 border-b hover:bg-slate-50 transition ${selectedEmail?.id === email.id ? 'bg-blue-50' : ''
                                    } ${!email.isRead ? 'bg-blue-50/50' : ''}`}
                            >
                                <div className="flex items-start gap-2">
                                    <div className={`w-2 h-2 rounded-full mt-2 ${!email.isRead ? 'bg-blue-600' : 'bg-transparent'
                                        }`} />
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center justify-between">
                                            <span className={`text-sm truncate ${!email.isRead ? 'font-semibold' : ''
                                                }`}>
                                                {email.fromName || email.fromAddress}
                                            </span>
                                            <span className="text-xs text-slate-400 flex-shrink-0 ml-2">
                                                {formatDate(email.receivedAt)}
                                            </span>
                                        </div>
                                        <p className={`text-sm truncate ${!email.isRead ? 'text-slate-800' : 'text-slate-600'
                                            }`}>
                                            {email.subject}
                                        </p>
                                        <div className="flex items-center gap-2 mt-1">
                                            {email.hasAttachments && (
                                                <Paperclip className="w-3 h-3 text-slate-400" />
                                            )}
                                            {email.importance === 'High' && (
                                                <span className="text-xs text-red-500">!</span>
                                            )}
                                            {(email.matterId || email.clientId) && (
                                                <Link2 className="w-3 h-3 text-green-500" />
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </button>
                        ))
                    )}
                </div>
            </div>

            {/* Email Detail */}
            <div className="flex-1 flex flex-col">
                {selectedEmail ? (
                    <>
                        {/* Email Header */}
                        <div className="px-6 py-4 border-b">
                            <h3 className="text-lg font-semibold text-slate-800 mb-2">
                                {selectedEmail.subject}
                            </h3>
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm">
                                        <span className="font-medium">{selectedEmail.fromName}</span>
                                        <span className="text-slate-500"> &lt;{selectedEmail.fromAddress}&gt;</span>
                                    </p>
                                    <p className="text-xs text-slate-500">
                                        To: {selectedEmail.toAddresses}
                                    </p>
                                </div>
                                <div className="flex items-center gap-2">
                                    <button
                                        onClick={() => {
                                            setLinkingEmail(selectedEmail);
                                            setShowLinkModal(true);
                                        }}
                                        className="px-3 py-1.5 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 flex items-center gap-1"
                                    >
                                        <Link2 className="w-4 h-4" />
                                        Link
                                    </button>
                                </div>
                            </div>
                        </div>

                        {/* Email Body */}
                        <div className="flex-1 overflow-auto p-6">
                            {selectedEmail.bodyHtml ? (
                                <div
                                    className="prose prose-sm max-w-none"
                                    dangerouslySetInnerHTML={{ __html: selectedEmail.bodyHtml }}
                                />
                            ) : (
                                <pre className="text-sm text-slate-600 whitespace-pre-wrap font-sans">
                                    {selectedEmail.bodyText || 'No content'}
                                </pre>
                            )}
                        </div>
                    </>
                ) : (
                    <div className="flex-1 flex items-center justify-center text-slate-400">
                        <div className="text-center">
                            <Mail className="w-16 h-16 mx-auto mb-4 opacity-30" />
                            <p>Select an email to read</p>
                        </div>
                    </div>
                )}
            </div>

            {/* Link Modal */}
            {showLinkModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
                    <div className="bg-white rounded-xl p-6 w-full max-w-md">
                        <h3 className="text-lg font-semibold mb-4">Link Email to Matter</h3>
                        <p className="text-sm text-slate-600 mb-4">
                            Select a matter to link this email to, or use auto-link to match by email address.
                        </p>
                        <div className="flex gap-2">
                            <button
                                onClick={() => handleLinkEmail(matterId, clientId)}
                                disabled={!matterId && !clientId}
                                className="flex-1 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                            >
                                Link to Current
                            </button>
                            <button
                                onClick={() => setShowLinkModal(false)}
                                className="px-4 py-2 border border-slate-200 rounded-lg hover:bg-slate-50"
                            >
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
