'use client';

import { useState, useEffect } from 'react';
import { FileText, AlertTriangle, CheckCircle, Clock, Users, Calendar, Shield, Sparkles, ChevronDown, ChevronUp } from './Icons';
import { api } from '../services/api';

interface ContractAnalysis {
    id: string;
    documentId: string;
    contractType: string;
    summary?: string;
    keyTerms?: Array<{ key: string; value: string }>;
    keyDates?: Array<{ key: string; value: string }>;
    parties?: string[];
    risks?: Array<{ level: string; description: string }>;
    riskScore: number;
    unusualClauses?: string[];
    recommendations?: string[];
    status: string;
    createdAt: string;
}

interface ContractAnalyzerProps {
    documentId: string;
    documentContent?: string;
    matterId?: string;
    contractType?: string;
    onAnalysisComplete?: (analysis: ContractAnalysis) => void;
}

export default function ContractAnalyzer({
    documentId,
    documentContent,
    matterId,
    contractType,
    onAnalysisComplete
}: ContractAnalyzerProps) {
    const [analysis, setAnalysis] = useState<ContractAnalysis | null>(null);
    const [analyzing, setAnalyzing] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(['summary', 'risks']));

    useEffect(() => {
        loadExistingAnalysis();
    }, [documentId]);

    const loadExistingAnalysis = async () => {
        try {
            const analyses = await api.ai.contracts.list({ documentId });
            if (analyses.length > 0) {
                const latest = analyses[0];
                setAnalysis(parseAnalysis(latest));
            }
        } catch (err) {
            console.error('Failed to load analysis:', err);
        }
    };

    const parseAnalysis = (data: any): ContractAnalysis => ({
        ...data,
        keyTerms: data.keyTermsJson ? JSON.parse(data.keyTermsJson) : [],
        keyDates: data.keyDatesJson ? JSON.parse(data.keyDatesJson) : [],
        parties: data.partiesJson ? JSON.parse(data.partiesJson) : [],
        risks: data.risksJson ? JSON.parse(data.risksJson) : [],
        unusualClauses: data.unusualClausesJson ? JSON.parse(data.unusualClausesJson) : [],
        recommendations: data.recommendationsJson ? JSON.parse(data.recommendationsJson) : []
    });

    const handleAnalyze = async () => {
        if (!documentContent) {
            setError('Document content is required for analysis');
            return;
        }

        setAnalyzing(true);
        setError(null);

        try {
            const response = await api.ai.contracts.analyze({
                documentId,
                documentContent,
                matterId,
                contractType
            });

            const parsed = parseAnalysis(response);
            setAnalysis(parsed);
            onAnalysisComplete?.(parsed);
        } catch (err) {
            setError('Failed to analyze contract');
            console.error(err);
        } finally {
            setAnalyzing(false);
        }
    };

    const toggleSection = (section: string) => {
        const next = new Set(expandedSections);
        if (next.has(section)) {
            next.delete(section);
        } else {
            next.add(section);
        }
        setExpandedSections(next);
    };

    const getRiskColor = (score: number) => {
        if (score <= 3) return { bg: 'bg-green-100', text: 'text-green-700', label: 'Low Risk' };
        if (score <= 6) return { bg: 'bg-amber-100', text: 'text-amber-700', label: 'Medium Risk' };
        return { bg: 'bg-red-100', text: 'text-red-700', label: 'High Risk' };
    };

    const getRiskLevelColor = (level: string) => {
        switch (level.toLowerCase()) {
            case 'high': return 'text-red-600 bg-red-50';
            case 'medium': return 'text-amber-600 bg-amber-50';
            default: return 'text-green-600 bg-green-50';
        }
    };

    if (!analysis && !analyzing) {
        return (
            <div className="bg-white rounded-xl border border-slate-200 p-6 text-center">
                <div className="w-16 h-16 bg-gradient-to-br from-blue-100 to-indigo-100 rounded-full flex items-center justify-center mx-auto mb-4">
                    <Sparkles className="w-8 h-8 text-indigo-600" />
                </div>
                <h3 className="text-lg font-semibold text-slate-800 mb-2">AI Contract Analysis</h3>
                <p className="text-sm text-slate-500 mb-4">
                    Analyze this contract to identify key terms, risks, and recommendations.
                </p>
                {error && (
                    <p className="text-sm text-red-600 mb-4">{error}</p>
                )}
                <button
                    onClick={handleAnalyze}
                    disabled={!documentContent}
                    className="px-6 py-3 bg-gradient-to-r from-indigo-600 to-purple-600 text-white rounded-lg font-medium hover:from-indigo-700 hover:to-purple-700 disabled:opacity-50 transition"
                >
                    Analyze Contract
                </button>
            </div>
        );
    }

    if (analyzing) {
        return (
            <div className="bg-white rounded-xl border border-slate-200 p-8 text-center">
                <div className="w-16 h-16 border-4 border-indigo-200 border-t-indigo-600 rounded-full animate-spin mx-auto mb-4" />
                <h3 className="text-lg font-semibold text-slate-800 mb-2">Analyzing Contract...</h3>
                <p className="text-sm text-slate-500">
                    AI is reviewing the contract for key terms, risks, and recommendations.
                </p>
            </div>
        );
    }

    if (!analysis) return null;

    const riskConfig = getRiskColor(analysis.riskScore);

    return (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
            {/* Header with Risk Score */}
            <div className="px-6 py-4 border-b bg-gradient-to-r from-indigo-50 to-purple-50">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center">
                            <FileText className="w-5 h-5 text-white" />
                        </div>
                        <div>
                            <h2 className="text-lg font-semibold text-slate-800">Contract Analysis</h2>
                            <p className="text-sm text-slate-500">{analysis.contractType}</p>
                        </div>
                    </div>
                    <div className={`px-4 py-2 rounded-full ${riskConfig.bg}`}>
                        <div className="flex items-center gap-2">
                            <Shield className={`w-4 h-4 ${riskConfig.text}`} />
                            <span className={`font-medium ${riskConfig.text}`}>
                                {analysis.riskScore}/10 - {riskConfig.label}
                            </span>
                        </div>
                    </div>
                </div>
            </div>

            {/* Summary */}
            <CollapsibleSection
                title="Summary"
                isOpen={expandedSections.has('summary')}
                onToggle={() => toggleSection('summary')}
            >
                <p className="text-slate-600">{analysis.summary}</p>
            </CollapsibleSection>

            {/* Parties */}
            {analysis.parties && analysis.parties.length > 0 && (
                <CollapsibleSection
                    title="Parties"
                    icon={<Users className="w-4 h-4 text-blue-500" />}
                    isOpen={expandedSections.has('parties')}
                    onToggle={() => toggleSection('parties')}
                >
                    <ul className="space-y-1">
                        {analysis.parties.map((party, i) => (
                            <li key={i} className="text-slate-600 flex items-center gap-2">
                                <span className="w-2 h-2 bg-blue-400 rounded-full" />
                                {party}
                            </li>
                        ))}
                    </ul>
                </CollapsibleSection>
            )}

            {/* Key Terms */}
            {analysis.keyTerms && analysis.keyTerms.length > 0 && (
                <CollapsibleSection
                    title="Key Terms"
                    isOpen={expandedSections.has('terms')}
                    onToggle={() => toggleSection('terms')}
                >
                    <div className="space-y-2">
                        {analysis.keyTerms.map((term, i) => (
                            <div key={i} className="flex justify-between py-2 border-b border-slate-100 last:border-0">
                                <span className="font-medium text-slate-700">{term.key || (term as any).Key}</span>
                                <span className="text-slate-600">{term.value || (term as any).Value}</span>
                            </div>
                        ))}
                    </div>
                </CollapsibleSection>
            )}

            {/* Key Dates */}
            {analysis.keyDates && analysis.keyDates.length > 0 && (
                <CollapsibleSection
                    title="Key Dates"
                    icon={<Calendar className="w-4 h-4 text-amber-500" />}
                    isOpen={expandedSections.has('dates')}
                    onToggle={() => toggleSection('dates')}
                >
                    <div className="space-y-2">
                        {analysis.keyDates.map((date, i) => (
                            <div key={i} className="flex justify-between py-2 border-b border-slate-100 last:border-0">
                                <span className="font-medium text-slate-700">{date.key || (date as any).Key}</span>
                                <span className="text-slate-600">{date.value || (date as any).Value}</span>
                            </div>
                        ))}
                    </div>
                </CollapsibleSection>
            )}

            {/* Risks */}
            {analysis.risks && analysis.risks.length > 0 && (
                <CollapsibleSection
                    title="Identified Risks"
                    icon={<AlertTriangle className="w-4 h-4 text-red-500" />}
                    isOpen={expandedSections.has('risks')}
                    onToggle={() => toggleSection('risks')}
                    bgColor="bg-red-50/50"
                >
                    <ul className="space-y-2">
                        {analysis.risks.map((risk, i) => (
                            <li key={i} className="flex items-start gap-3">
                                <span className={`px-2 py-0.5 rounded text-xs font-medium ${getRiskLevelColor(risk.level || (risk as any).Level)}`}>
                                    {risk.level || (risk as any).Level}
                                </span>
                                <span className="text-slate-600">{risk.description || (risk as any).Description}</span>
                            </li>
                        ))}
                    </ul>
                </CollapsibleSection>
            )}

            {/* Unusual Clauses */}
            {analysis.unusualClauses && analysis.unusualClauses.length > 0 && (
                <CollapsibleSection
                    title="Unusual Clauses"
                    isOpen={expandedSections.has('unusual')}
                    onToggle={() => toggleSection('unusual')}
                    bgColor="bg-amber-50/50"
                >
                    <ul className="space-y-2">
                        {analysis.unusualClauses.map((clause, i) => (
                            <li key={i} className="text-slate-600 flex items-start gap-2">
                                <span className="text-amber-500">⚠️</span>
                                {clause}
                            </li>
                        ))}
                    </ul>
                </CollapsibleSection>
            )}

            {/* Recommendations */}
            {analysis.recommendations && analysis.recommendations.length > 0 && (
                <CollapsibleSection
                    title="Recommendations"
                    icon={<CheckCircle className="w-4 h-4 text-green-500" />}
                    isOpen={expandedSections.has('recommendations')}
                    onToggle={() => toggleSection('recommendations')}
                    bgColor="bg-green-50/50"
                >
                    <ul className="space-y-2">
                        {analysis.recommendations.map((rec, i) => (
                            <li key={i} className="text-slate-600 flex items-start gap-2">
                                <span className="w-5 h-5 rounded-full bg-green-100 text-green-600 flex items-center justify-center text-xs flex-shrink-0">
                                    {i + 1}
                                </span>
                                {rec}
                            </li>
                        ))}
                    </ul>
                </CollapsibleSection>
            )}

            {/* Disclaimer */}
            <div className="px-6 py-3 border-t bg-amber-50 text-xs text-amber-700">
                ⚠️ AI analysis is for informational purposes only. Always have contracts reviewed by qualified legal counsel.
            </div>
        </div>
    );
}

// Collapsible Section Component
function CollapsibleSection({
    title,
    icon,
    isOpen,
    onToggle,
    bgColor,
    children
}: {
    title: string;
    icon?: React.ReactNode;
    isOpen: boolean;
    onToggle: () => void;
    bgColor?: string;
    children: React.ReactNode;
}) {
    return (
        <div className={`border-t ${bgColor || ''}`}>
            <button
                onClick={onToggle}
                className="w-full px-6 py-3 flex items-center justify-between hover:bg-slate-50 transition"
            >
                <span className="font-medium text-slate-800 flex items-center gap-2">
                    {icon}
                    {title}
                </span>
                {isOpen ? (
                    <ChevronUp className="w-4 h-4 text-slate-400" />
                ) : (
                    <ChevronDown className="w-4 h-4 text-slate-400" />
                )}
            </button>
            {isOpen && (
                <div className="px-6 pb-4">
                    {children}
                </div>
            )}
        </div>
    );
}
