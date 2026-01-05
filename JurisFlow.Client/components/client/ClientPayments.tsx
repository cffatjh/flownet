import React, { useState, useEffect } from 'react';
import { useClientAuth } from '../../contexts/ClientAuthContext';
import { Invoice, InvoiceStatus } from '../../types';
import PaymentCheckout from '../PaymentCheckout';
import PaymentHistory from '../PaymentHistory';
import { CreditCard, Check, AlertCircle, Clock, DollarSign, Wallet, Receipt } from '../Icons';

interface ClientPaymentsProps {
    clientId: string;
}

const ClientPayments: React.FC<ClientPaymentsProps> = ({ clientId }) => {
    const { client } = useClientAuth();
    const [invoices, setInvoices] = useState<Invoice[]>([]);
    const [loading, setLoading] = useState(true);
    const [checkoutInvoice, setCheckoutInvoice] = useState<Invoice | null>(null);
    const [showCheckout, setShowCheckout] = useState(false);

    const clientToken = typeof window !== 'undefined' ? localStorage.getItem('client_token') : null;

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        try {
            const token = localStorage.getItem('client_token');
            const res = await fetch('/api/client/invoices', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setInvoices(data);
            }
        } catch (error) {
            console.error('Error fetching invoices:', error);
        } finally {
            setLoading(false);
        }
    };

    const getBalance = (invoice: Invoice) => {
        if (typeof invoice.balance === 'number') return Math.max(0, invoice.balance);
        if (typeof invoice.amountPaid === 'number') {
            return Math.max(0, invoice.amount - invoice.amountPaid);
        }
        return invoice.amount;
    };

    const handleCheckout = (invoice: Invoice) => {
        setCheckoutInvoice(invoice);
        setShowCheckout(true);
    };

    const handlePaymentSuccess = () => {
        if (!checkoutInvoice) return;
        setInvoices(prev => prev.map(inv => {
            if (inv.id !== checkoutInvoice.id) return inv;
            return {
                ...inv,
                status: InvoiceStatus.PAID,
                amountPaid: inv.amount,
                balance: 0
            };
        }));
    };

    const formatCurrency = (amount: number) => {
        return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
    };

    const formatDate = (dateStr?: string) => {
        if (!dateStr) return 'N/A';
        return new Date(dateStr).toLocaleDateString('en-US');
    };

    const getStatusBadge = (status?: string) => {
        const normalized = status?.toLowerCase();
        switch (normalized) {
            case 'paid':
                return <span className="px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800 flex items-center gap-1"><Check className="w-3 h-3" /> Paid</span>;
            case 'overdue':
                return <span className="px-2 py-1 text-xs font-medium rounded-full bg-red-100 text-red-800 flex items-center gap-1"><AlertCircle className="w-3 h-3" /> Overdue</span>;
            case 'sent':
                return <span className="px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-800 flex items-center gap-1"><Clock className="w-3 h-3" /> Sent</span>;
            case 'draft':
                return <span className="px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-800">Draft</span>;
            default:
                return null;
        }
    };

    const unpaidInvoices = invoices.filter(inv => {
        const status = inv.status?.toLowerCase();
        if (status === 'draft') return false;
        return getBalance(inv) > 0;
    });

    const paidInvoices = invoices.filter(inv => {
        const status = inv.status?.toLowerCase();
        return status === 'paid' || getBalance(inv) === 0;
    });

    if (loading) {
        return (
            <div className="flex items-center justify-center h-64">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            </div>
        );
    }

    return (
        <div className="p-6 h-full overflow-auto">
            <div className="max-w-5xl mx-auto">
                {/* Header */}
                <div className="mb-6">
                    <h2 className="text-2xl font-bold text-gray-900">Payments</h2>
                    <p className="text-gray-600 mt-1">Review invoices, choose a payment method, and complete your balance</p>
                </div>

                {/* Summary Cards */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
                        <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-full bg-red-100 flex items-center justify-center">
                                <AlertCircle className="w-5 h-5 text-red-600" />
                            </div>
                            <div>
                                <p className="text-sm text-gray-500">Outstanding</p>
                                <p className="text-xl font-bold text-gray-900">
                                    {formatCurrency(unpaidInvoices.reduce((sum, inv) => sum + getBalance(inv), 0))}
                                </p>
                            </div>
                        </div>
                    </div>
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
                        <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-full bg-green-100 flex items-center justify-center">
                                <Check className="w-5 h-5 text-green-600" />
                            </div>
                            <div>
                                <p className="text-sm text-gray-500">Paid</p>
                                <p className="text-xl font-bold text-gray-900">
                                    {formatCurrency(paidInvoices.reduce((sum, inv) => sum + (inv.amountPaid || inv.amount), 0))}
                                </p>
                            </div>
                        </div>
                    </div>
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
                        <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center">
                                <CreditCard className="w-5 h-5 text-blue-600" />
                            </div>
                            <div>
                                <p className="text-sm text-gray-500">Total Invoices</p>
                                <p className="text-xl font-bold text-gray-900">{invoices.length}</p>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Payment Options */}
                <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-5 mb-8">
                    <div className="flex items-center justify-between">
                        <div>
                            <h3 className="text-lg font-semibold text-gray-900">Payment Options</h3>
                            <p className="text-sm text-gray-500 mt-1">Choose the method that works best for you. Online payments complete instantly.</p>
                        </div>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-4">
                        <div className="border border-gray-200 rounded-lg p-4">
                            <div className="flex items-center gap-2 text-sm font-semibold text-gray-900">
                                <CreditCard className="w-4 h-4 text-blue-600" />
                                Card Payment
                            </div>
                            <p className="text-sm text-gray-500 mt-2">Pay with credit or debit card for immediate confirmation.</p>
                        </div>
                        <div className="border border-gray-200 rounded-lg p-4">
                            <div className="flex items-center gap-2 text-sm font-semibold text-gray-900">
                                <Wallet className="w-4 h-4 text-emerald-600" />
                                Bank Transfer
                            </div>
                            <p className="text-sm text-gray-500 mt-2">Wire or ACH details provided by your firm.</p>
                        </div>
                        <div className="border border-gray-200 rounded-lg p-4">
                            <div className="flex items-center gap-2 text-sm font-semibold text-gray-900">
                                <Receipt className="w-4 h-4 text-amber-600" />
                                Check by Mail
                            </div>
                            <p className="text-sm text-gray-500 mt-2">Mailing instructions are available from your firm.</p>
                        </div>
                    </div>
                </div>

                {/* Unpaid Invoices */}
                {unpaidInvoices.length > 0 && (
                    <div className="mb-8">
                        <h3 className="text-lg font-semibold text-gray-900 mb-4">Outstanding Invoices</h3>
                        <div className="space-y-3">
                            {unpaidInvoices.map((invoice) => (
                                <div key={invoice.id} className="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
                                    <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                                        <div className="flex items-center gap-4">
                                            <div className="w-12 h-12 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-bold">
                                                <DollarSign className="w-6 h-6" />
                                            </div>
                                            <div>
                                                <div className="flex items-center gap-3">
                                                    <span className="font-semibold text-gray-900">Invoice #{invoice.number}</span>
                                                    {getStatusBadge(invoice.status)}
                                                </div>
                                                <p className="text-sm text-gray-500">Due date: {formatDate(invoice.dueDate)}</p>
                                                <p className="text-xs text-gray-400 mt-1">Balance due: {formatCurrency(getBalance(invoice))}</p>
                                            </div>
                                        </div>
                                        <div className="flex items-center gap-4">
                                            <span className="text-xl font-bold text-gray-900">{formatCurrency(getBalance(invoice))}</span>
                                            <button
                                                onClick={() => handleCheckout(invoice)}
                                                disabled={getBalance(invoice) <= 0}
                                                className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
                                            >
                                                <CreditCard className="w-4 h-4" />
                                                Pay Now
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* Paid Invoices */}
                {paidInvoices.length > 0 && (
                    <div className="mb-8">
                        <h3 className="text-lg font-semibold text-gray-900 mb-4">Paid Invoices</h3>
                        <div className="space-y-3">
                            {paidInvoices.map((invoice) => (
                                <div key={invoice.id} className="bg-white rounded-xl shadow-sm border border-gray-200 p-5 opacity-80">
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center gap-4">
                                            <div className="w-12 h-12 rounded-full bg-green-100 flex items-center justify-center">
                                                <Check className="w-6 h-6 text-green-600" />
                                            </div>
                                            <div>
                                                <div className="flex items-center gap-3">
                                                    <span className="font-semibold text-gray-900">Invoice #{invoice.number}</span>
                                                    {getStatusBadge(invoice.status)}
                                                </div>
                                                <p className="text-sm text-gray-500">Paid on: {formatDate(invoice.paidDate || invoice.updatedAt || invoice.dueDate)}</p>
                                            </div>
                                        </div>
                                        <span className="text-xl font-bold text-gray-900">{formatCurrency(invoice.amount)}</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* Payment History */}
                {clientId && (
                    <div className="mb-8">
                        <PaymentHistory clientId={clientId} authToken={clientToken || undefined} />
                    </div>
                )}

                {/* Empty State */}
                {invoices.length === 0 && (
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
                        <CreditCard className="w-16 h-16 text-gray-300 mx-auto mb-4" />
                        <h3 className="text-lg font-medium text-gray-900 mb-2">No invoices yet</h3>
                        <p className="text-gray-500">Invoices from your attorney will appear here.</p>
                    </div>
                )}
            </div>

            <PaymentCheckout
                isOpen={showCheckout}
                onClose={() => setShowCheckout(false)}
                invoiceId={checkoutInvoice?.id}
                invoiceNumber={checkoutInvoice?.number}
                clientId={client?.id}
                amount={checkoutInvoice ? getBalance(checkoutInvoice) : 0}
                clientName={client?.name}
                clientEmail={client?.email}
                mode="demo"
                authToken={clientToken || undefined}
                onSuccess={handlePaymentSuccess}
            />
        </div>
    );
};

export default ClientPayments;
