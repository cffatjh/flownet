'use client';

import { useState } from 'react';
import { X, AlertTriangle, CheckCircle, Search, Shield, AlertCircle } from './Icons';
import { api } from '../services/api';

interface ConflictResult {
    id: string;
    matchedEntityType: string;
    matchedEntityId: string;
    matchedEntityName: string;
    matchType: string;
    matchScore: number;
    riskLevel: string;
    relatedMatterId?: string;
    relatedMatterName?: string;
}

interface ConflictCheckResult {
    id: string;
    status: string;
    matchCount: number;
    results: ConflictResult[];
}

interface ConflictCheckModalProps {
    isOpen: boolean;
    onClose: () => void;
    entityType: 'Client' | 'Matter' | 'OpposingParty';
    entityName?: string;
    entityId?: string;
    onClear?: () => void;
    onWaive?: (checkId: string, reason: string) => void;
}

export default function ConflictCheckModal({
    isOpen,
    onClose,
    entityType,
    entityName = '',
    entityId,
    onClear,
    onWaive
}: ConflictCheckModalProps) {
    const [searchQuery, setSearchQuery] = useState(entityName);
    const [loading, setLoading] = useState(false);
    const [result, setResult] = useState<ConflictCheckResult | null>(null);
    const [waiveReason, setWaiveReason] = useState('');
    const [showWaiveForm, setShowWaiveForm] = useState(false);

    const handleCheck = async () => {
        if (!searchQuery.trim()) return;

        setLoading(true);
        try {
            const response = await api.conflicts.check({
                searchQuery: searchQuery.trim(),
                checkType: `New${entityType}`,
                entityType,
                entityId
            });
            setResult(response);
        } catch (error) {
            console.error('Conflict check failed:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleWaive = async () => {
        if (!result || !waiveReason.trim()) return;

        try {
            await api.conflicts.waive(result.id, waiveReason);
            onWaive?.(result.id, waiveReason);
            onClose();
        } catch (error) {
            console.error('Failed to waive conflict:', error);
        }
    };

    const handleClear = () => {
        onClear?.();
        onClose();
    };

    const getRiskBadge = (riskLevel: string) => {
        switch (riskLevel) {
            case 'High':
                return <span className="px-2 py-1 text-xs font-medium bg-red-100 text-red-700 rounded-full">High Risk</span>;
            case 'Medium':
                return <span className="px-2 py-1 text-xs font-medium bg-yellow-100 text-yellow-700 rounded-full">Medium Risk</span>;
            default:
                return <span className="px-2 py-1 text-xs font-medium bg-blue-100 text-blue-700 rounded-full">Low Risk</span>;
        }
    };

    const getEntityIcon = (type: string) => {
        switch (type) {
            case 'Client':
                return '👤';
            case 'Matter':
                return '📁';
            case 'OpposingParty':
                return '⚖️';
            default:
                return '📋';
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-hidden">
                {/* Header */}
                <div className="flex items-center justify-between p-4 border-b bg-gradient-to-r from-amber-50 to-orange-50">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-full bg-amber-100 flex items-center justify-center">
                            <Shield className="w-5 h-5 text-amber-600" />
                        </div>
                        <div>
                            <h2 className="text-lg font-semibold text-slate-800">Conflict of Interest Check</h2>
                            <p className="text-sm text-slate-500">ABA Model Rule 1.7 Compliance</p>
                        </div>
                    </div>
                    <button onClick={onClose} className="p-2 hover:bg-white/50 rounded-full transition">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                {/* Content */}
                <div className="p-6 overflow-y-auto max-h-[calc(90vh-140px)]">
                    {/* Search Form */}
                    <div className="mb-6">
                        <label className="block text-sm font-medium text-slate-700 mb-2">
                            Search Name / Entity
                        </label>
                        <div className="flex gap-2">
                            <input
                                type="text"
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                placeholder="Enter name, company, or email to check..."
                                className="flex-1 px-4 py-2 border border-slate-200 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                                onKeyDown={(e) => e.key === 'Enter' && handleCheck()}
                            />
                            <button
                                onClick={handleCheck}
                                disabled={loading || !searchQuery.trim()}
                                className="px-4 py-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 disabled:opacity-50 flex items-center gap-2"
                            >
                                {loading ? (
                                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                ) : (
                                    <Search className="w-5 h-5" />
                                )}
                                Check
                            </button>
                        </div>
                    </div>

                    {/* Results */}
                    {result && (
                        <div className="space-y-4">
                            {/* Status Banner */}
                            {result.status === 'Clear' ? (
                                <div className="flex items-center gap-3 p-4 bg-green-50 border border-green-200 rounded-lg">
                                    <CheckCircle className="w-6 h-6 text-green-600" />
                                    <div>
                                        <p className="font-medium text-green-800">No Conflicts Found</p>
                                        <p className="text-sm text-green-600">You may proceed with this {entityType.toLowerCase()}.</p>
                                    </div>
                                </div>
                            ) : (
                                <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
                                    <AlertTriangle className="w-6 h-6 text-red-600" />
                                    <div>
                                        <p className="font-medium text-red-800">
                                            {result.matchCount} Potential Conflict{result.matchCount !== 1 ? 's' : ''} Found
                                        </p>
                                        <p className="text-sm text-red-600">Review the matches below before proceeding.</p>
                                    </div>
                                </div>
                            )}

                            {/* Match List */}
                            {result.results.length > 0 && (
                                <div className="space-y-3">
                                    <h3 className="font-medium text-slate-700">Matched Entities</h3>
                                    {result.results.map((match) => (
                                        <div
                                            key={match.id}
                                            className="p-4 bg-slate-50 border border-slate-200 rounded-lg hover:bg-slate-100 transition"
                                        >
                                            <div className="flex items-start justify-between">
                                                <div className="flex items-start gap-3">
                                                    <span className="text-2xl">{getEntityIcon(match.matchedEntityType)}</span>
                                                    <div>
                                                        <p className="font-medium text-slate-800">{match.matchedEntityName}</p>
                                                        <p className="text-sm text-slate-500">
                                                            {match.matchedEntityType} • {match.matchType} Match ({match.matchScore}%)
                                                        </p>
                                                        {match.relatedMatterName && (
                                                            <p className="text-sm text-slate-600 mt-1">
                                                                Related Matter: {match.relatedMatterName}
                                                            </p>
                                                        )}
                                                    </div>
                                                </div>
                                                {getRiskBadge(match.riskLevel)}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}

                            {/* Waive Form */}
                            {result.status === 'Conflict' && (
                                <div className="mt-6 pt-4 border-t">
                                    {!showWaiveForm ? (
                                        <button
                                            onClick={() => setShowWaiveForm(true)}
                                            className="text-amber-600 hover:text-amber-700 text-sm font-medium flex items-center gap-2"
                                        >
                                            <AlertCircle className="w-4 h-4" />
                                            I have reviewed and wish to waive this conflict
                                        </button>
                                    ) : (
                                        <div className="space-y-3">
                                            <label className="block text-sm font-medium text-slate-700">
                                                Waiver Reason (Required)
                                            </label>
                                            <textarea
                                                value={waiveReason}
                                                onChange={(e) => setWaiveReason(e.target.value)}
                                                placeholder="Explain why this conflict can be waived (e.g., client consent obtained, different practice areas, etc.)"
                                                className="w-full px-4 py-2 border border-slate-200 rounded-lg focus:ring-2 focus:ring-amber-500 min-h-[100px]"
                                            />
                                            <p className="text-xs text-slate-500">
                                                ⚠️ This waiver will be logged for audit purposes per ABA Model Rule 1.7 requirements.
                                            </p>
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    )}
                </div>

                {/* Footer */}
                <div className="flex items-center justify-end gap-3 p-4 border-t bg-slate-50">
                    <button
                        onClick={onClose}
                        className="px-4 py-2 text-slate-600 hover:bg-slate-100 rounded-lg transition"
                    >
                        Cancel
                    </button>
                    {result?.status === 'Clear' && (
                        <button
                            onClick={handleClear}
                            className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition flex items-center gap-2"
                        >
                            <CheckCircle className="w-4 h-4" />
                            Proceed
                        </button>
                    )}
                    {result?.status === 'Conflict' && showWaiveForm && (
                        <button
                            onClick={handleWaive}
                            disabled={!waiveReason.trim()}
                            className="px-4 py-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 disabled:opacity-50 transition flex items-center gap-2"
                        >
                            <AlertTriangle className="w-4 h-4" />
                            Waive & Proceed
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
}
