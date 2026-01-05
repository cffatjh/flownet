'use client';

import { useState, useEffect } from 'react';
import { CreditCard, DollarSign, CheckCircle, XCircle, Clock, RefreshCw, RotateCcw, ExternalLink } from './Icons';

interface PaymentTransaction {
    id: string;
    invoiceId?: string;
    matterId?: string;
    clientId?: string;
    amount: number;
    currency: string;
    status: string;
    paymentMethod?: string;
    cardLast4?: string;
    cardBrand?: string;
    transactionDate?: string;
    processedAt?: string;
    refundedAt?: string;
    refundAmount?: number;
    receiptUrl?: string;
    createdAt: string;
}

interface PaymentHistoryProps {
    clientId?: string;
    matterId?: string;
    invoiceId?: string;
    limit?: number;
    showHeader?: boolean;
    authToken?: string;
}

export default function PaymentHistory({
    clientId,
    matterId,
    invoiceId,
    limit = 10,
    showHeader = true,
    authToken
}: PaymentHistoryProps) {
    const [payments, setPayments] = useState<PaymentTransaction[]>([]);
    const [loading, setLoading] = useState(true);
    const [stats, setStats] = useState<{ totalReceived: number; pending: number; refunded: number } | null>(null);

    useEffect(() => {
        loadPayments();
    }, [clientId, matterId, invoiceId]);

    const requestJson = async (endpoint: string) => {
        const token = authToken || (typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null);
        const res = await fetch(`/api${endpoint}`, {
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { Authorization: `Bearer ${token}` } : {})
            }
        });
        if (!res.ok) throw new Error(`API Error: ${res.statusText}`);
        return res.json();
    };

    const loadPayments = async () => {
        setLoading(true);
        try {
            let data;
            if (invoiceId) {
                data = await requestJson(`/payments/invoice/${invoiceId}`);
            } else if (matterId) {
                data = await requestJson(`/payments/matter/${matterId}`);
            } else if (clientId) {
                data = await requestJson(`/payments/client/${clientId}`);
            } else {
                data = [];
            }

            // Handle both array and paginated response
            setPayments(Array.isArray(data) ? data.slice(0, limit) : []);

            // Calculate stats
            const allPayments = Array.isArray(data) ? data : [];
            const totalReceived = allPayments
                .filter((p: PaymentTransaction) => p.status === 'Succeeded')
                .reduce((sum: number, p: PaymentTransaction) => sum + p.amount, 0);
            const pending = allPayments
                .filter((p: PaymentTransaction) => p.status === 'Pending')
                .reduce((sum: number, p: PaymentTransaction) => sum + p.amount, 0);
            const refunded = allPayments
                .reduce((sum: number, p: PaymentTransaction) => sum + (p.refundAmount || 0), 0);

            setStats({ totalReceived, pending, refunded });
        } catch (error) {
            console.error('Failed to load payments:', error);
        } finally {
            setLoading(false);
        }
    };

    const getStatusConfig = (status: string) => {
        switch (status) {
            case 'Succeeded':
                return { icon: CheckCircle, color: 'text-green-600', bg: 'bg-green-100', label: 'Paid' };
            case 'Failed':
                return { icon: XCircle, color: 'text-red-600', bg: 'bg-red-100', label: 'Failed' };
            case 'Pending':
                return { icon: Clock, color: 'text-amber-600', bg: 'bg-amber-100', label: 'Pending' };
            case 'Refunded':
                return { icon: RotateCcw, color: 'text-purple-600', bg: 'bg-purple-100', label: 'Refunded' };
            default:
                return { icon: Clock, color: 'text-slate-500', bg: 'bg-slate-100', label: status };
        }
    };

    const formatCurrency = (amount: number, currency: string = 'USD') => {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: currency
        }).format(amount);
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric'
        });
    };

    const getPaymentDate = (payment: PaymentTransaction) => {
        return payment.processedAt || payment.createdAt || payment.transactionDate || '';
    };

    const getCardIcon = (brand?: string) => {
        // Simple card brand display
        if (!brand) return null;
        return (
            <span className="text-xs text-slate-500">{brand}</span>
        );
    };

    if (loading) {
        return (
            <div className="bg-white rounded-xl border border-slate-200 p-6">
                <div className="flex items-center justify-center h-32">
                    <div className="w-6 h-6 border-2 border-blue-200 border-t-blue-600 rounded-full animate-spin" />
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
            {showHeader && (
                <div className="px-4 py-3 border-b bg-gradient-to-r from-green-50 to-emerald-50 flex items-center justify-between">
                    <div className="flex items-center gap-2">
                        <CreditCard className="w-5 h-5 text-green-600" />
                        <h3 className="font-semibold text-slate-800">Payment History</h3>
                    </div>
                    <button onClick={loadPayments} className="p-1.5 hover:bg-white/50 rounded transition">
                        <RefreshCw className="w-4 h-4" />
                    </button>
                </div>
            )}

            {/* Stats */}
            {stats && (stats.totalReceived > 0 || stats.pending > 0) && (
                <div className="grid grid-cols-3 divide-x border-b">
                    <div className="p-3 text-center">
                        <p className="text-xs text-slate-500">Received</p>
                        <p className="font-semibold text-green-600">{formatCurrency(stats.totalReceived)}</p>
                    </div>
                    <div className="p-3 text-center">
                        <p className="text-xs text-slate-500">Pending</p>
                        <p className="font-semibold text-amber-600">{formatCurrency(stats.pending)}</p>
                    </div>
                    <div className="p-3 text-center">
                        <p className="text-xs text-slate-500">Refunded</p>
                        <p className="font-semibold text-purple-600">{formatCurrency(stats.refunded)}</p>
                    </div>
                </div>
            )}

            {/* Payments List */}
            {payments.length === 0 ? (
                <div className="text-center py-12 text-slate-500">
                    <DollarSign className="w-10 h-10 mx-auto mb-2 opacity-30" />
                    <p className="text-sm">No payment history</p>
                </div>
            ) : (
                <div className="divide-y">
                    {payments.map(payment => {
                        const config = getStatusConfig(payment.status);
                        const Icon = config.icon;

                        return (
                            <div key={payment.id} className="p-4 hover:bg-slate-50 transition">
                                <div className="flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <div className={`w-10 h-10 rounded-full ${config.bg} flex items-center justify-center`}>
                                            <Icon className={`w-5 h-5 ${config.color}`} />
                                        </div>
                                        <div>
                                            <p className="font-medium text-slate-800">
                                                {formatCurrency(payment.amount, payment.currency)}
                                            </p>
                                            <div className="flex items-center gap-2 text-xs text-slate-500">
                                                <span>{formatDate(getPaymentDate(payment))}</span>
                                                {payment.cardLast4 && (
                                                    <>
                                                        <span className="text-slate-300">|</span>
                                                        <span className="flex items-center gap-1">
                                                            {getCardIcon(payment.cardBrand)}
                                                            ****{payment.cardLast4}
                                                        </span>
                                                    </>
                                                )}
                                            </div>
                                            {payment.refundAmount && payment.refundAmount > 0 && (
                                                <p className="text-xs text-purple-600 mt-1">
                                                    Refunded: {formatCurrency(payment.refundAmount)}
                                                </p>
                                            )}
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-2">
                                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${config.bg} ${config.color}`}>
                                            {config.label}
                                        </span>
                                        {payment.receiptUrl && (
                                            <a
                                                href={payment.receiptUrl}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded"
                                                title="View receipt"
                                            >
                                                <ExternalLink className="w-4 h-4" />
                                            </a>
                                        )}
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
}
