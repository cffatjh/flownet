'use client';

import { useState, useEffect } from 'react';
import { TrendingUp, Scale, DollarSign, Clock, AlertTriangle, CheckCircle, RefreshCw, ChevronRight, Sparkles } from './Icons';
import { api } from '../services/api';

interface CasePrediction {
    id: string;
    matterId: string;
    predictedOutcome: string;
    confidence: number;
    factors?: string[];
    similarCases?: string[];
    settlementMin?: number;
    settlementMax?: number;
    estimatedTimeline?: string;
    recommendations?: string[];
    status: string;
    createdAt: string;
}

interface CasePredictionProps {
    matterId: string;
    matterName?: string;
    practiceArea?: string;
}

export default function CasePredictionWidget({ matterId, matterName, practiceArea }: CasePredictionProps) {
    const [prediction, setPrediction] = useState<CasePrediction | null>(null);
    const [predicting, setPredicting] = useState(false);
    const [additionalContext, setAdditionalContext] = useState('');
    const [showDetails, setShowDetails] = useState(false);
    const [previousPredictions, setPreviousPredictions] = useState<CasePrediction[]>([]);

    useEffect(() => {
        loadPredictions();
    }, [matterId]);

    const loadPredictions = async () => {
        try {
            const predictions = await api.ai.predictions.list(matterId);
            if (predictions.length > 0) {
                const latest = parsePrediction(predictions[0]);
                setPrediction(latest);
                setPreviousPredictions(predictions.slice(1).map(parsePrediction));
            }
        } catch (error) {
            console.error('Failed to load predictions:', error);
        }
    };

    const parsePrediction = (data: any): CasePrediction => ({
        ...data,
        factors: data.factorsJson ? JSON.parse(data.factorsJson) : [],
        similarCases: data.similarCasesJson ? JSON.parse(data.similarCasesJson) : [],
        recommendations: data.recommendationsJson ? JSON.parse(data.recommendationsJson) : []
    });

    const handlePredict = async () => {
        setPredicting(true);
        try {
            const response = await api.ai.predictions.predict({
                matterId,
                additionalContext: additionalContext || undefined
            });
            const parsed = parsePrediction(response);
            setPrediction(parsed);
            setAdditionalContext('');
            await loadPredictions();
        } catch (error) {
            console.error('Failed to predict:', error);
        } finally {
            setPredicting(false);
        }
    };

    const getOutcomeConfig = (outcome: string) => {
        switch (outcome.toLowerCase()) {
            case 'win':
                return { color: 'text-green-600', bg: 'bg-green-100', icon: CheckCircle, label: 'Likely Win' };
            case 'lose':
                return { color: 'text-red-600', bg: 'bg-red-100', icon: AlertTriangle, label: 'Likely Lose' };
            case 'settlement':
                return { color: 'text-blue-600', bg: 'bg-blue-100', icon: Scale, label: 'Settlement' };
            default:
                return { color: 'text-slate-600', bg: 'bg-slate-100', icon: Scale, label: outcome };
        }
    };

    const getConfidenceColor = (confidence: number) => {
        if (confidence >= 70) return 'text-green-600';
        if (confidence >= 50) return 'text-amber-600';
        return 'text-red-600';
    };

    const formatCurrency = (amount?: number) => {
        if (!amount) return '-';
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD',
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(amount);
    };

    if (!prediction && !predicting) {
        return (
            <div className="bg-white rounded-xl border border-slate-200 p-6">
                <div className="text-center">
                    <div className="w-16 h-16 bg-gradient-to-br from-orange-100 to-amber-100 rounded-full flex items-center justify-center mx-auto mb-4">
                        <TrendingUp className="w-8 h-8 text-amber-600" />
                    </div>
                    <h3 className="text-lg font-semibold text-slate-800 mb-2">AI Case Prediction</h3>
                    <p className="text-sm text-slate-500 mb-4">
                        Get AI-powered outcome predictions and strategic insights for this case.
                    </p>
                    <div className="mb-4">
                        <textarea
                            value={additionalContext}
                            onChange={(e) => setAdditionalContext(e.target.value)}
                            placeholder="Optional: Add context about the case (evidence, opposing counsel, etc.)"
                            rows={2}
                            className="w-full px-4 py-2 border border-slate-200 rounded-lg text-sm"
                        />
                    </div>
                    <button
                        onClick={handlePredict}
                        className="px-6 py-3 bg-gradient-to-r from-orange-500 to-amber-500 text-white rounded-lg font-medium hover:from-orange-600 hover:to-amber-600 transition flex items-center gap-2 mx-auto"
                    >
                        <Sparkles className="w-5 h-5" />
                        Generate Prediction
                    </button>
                </div>
            </div>
        );
    }

    if (predicting) {
        return (
            <div className="bg-white rounded-xl border border-slate-200 p-8 text-center">
                <div className="w-16 h-16 border-4 border-amber-200 border-t-amber-600 rounded-full animate-spin mx-auto mb-4" />
                <h3 className="text-lg font-semibold text-slate-800 mb-2">Analyzing Case...</h3>
                <p className="text-sm text-slate-500">
                    AI is analyzing case factors and similar precedents.
                </p>
            </div>
        );
    }

    if (!prediction) return null;

    const outcomeConfig = getOutcomeConfig(prediction.predictedOutcome);
    const OutcomeIcon = outcomeConfig.icon;

    return (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
            {/* Header */}
            <div className="px-6 py-4 border-b bg-gradient-to-r from-orange-50 to-amber-50">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-orange-500 to-amber-500 flex items-center justify-center">
                            <TrendingUp className="w-5 h-5 text-white" />
                        </div>
                        <div>
                            <h2 className="font-semibold text-slate-800">Case Prediction</h2>
                            <p className="text-xs text-slate-500">
                                {new Date(prediction.createdAt).toLocaleDateString()}
                            </p>
                        </div>
                    </div>
                    <button
                        onClick={handlePredict}
                        disabled={predicting}
                        className="p-2 hover:bg-white/50 rounded-lg transition"
                        title="Refresh prediction"
                    >
                        <RefreshCw className={`w-4 h-4 ${predicting ? 'animate-spin' : ''}`} />
                    </button>
                </div>
            </div>

            {/* Main Prediction */}
            <div className="p-6">
                <div className="flex items-center justify-between mb-6">
                    <div className="flex items-center gap-4">
                        <div className={`w-14 h-14 rounded-full ${outcomeConfig.bg} flex items-center justify-center`}>
                            <OutcomeIcon className={`w-7 h-7 ${outcomeConfig.color}`} />
                        </div>
                        <div>
                            <p className="text-sm text-slate-500">Predicted Outcome</p>
                            <p className={`text-2xl font-bold ${outcomeConfig.color}`}>
                                {outcomeConfig.label}
                            </p>
                        </div>
                    </div>
                    <div className="text-right">
                        <p className="text-sm text-slate-500">Confidence</p>
                        <p className={`text-3xl font-bold ${getConfidenceColor(prediction.confidence)}`}>
                            {prediction.confidence.toFixed(1)}%
                        </p>
                    </div>
                </div>

                {/* Settlement Range */}
                {(prediction.settlementMin || prediction.settlementMax) && (
                    <div className="mb-6 p-4 bg-blue-50 rounded-lg">
                        <div className="flex items-center gap-2 mb-2">
                            <DollarSign className="w-5 h-5 text-blue-600" />
                            <span className="font-medium text-blue-800">Estimated Settlement Range</span>
                        </div>
                        <p className="text-2xl font-bold text-blue-700">
                            {formatCurrency(prediction.settlementMin)} - {formatCurrency(prediction.settlementMax)}
                        </p>
                    </div>
                )}

                {/* Timeline */}
                {prediction.estimatedTimeline && (
                    <div className="mb-6 p-4 bg-slate-50 rounded-lg flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            <Clock className="w-5 h-5 text-slate-600" />
                            <span className="font-medium text-slate-700">Estimated Timeline</span>
                        </div>
                        <span className="text-lg font-semibold text-slate-800">
                            {prediction.estimatedTimeline}
                        </span>
                    </div>
                )}

                {/* Toggle Details */}
                <button
                    onClick={() => setShowDetails(!showDetails)}
                    className="w-full py-2 text-sm text-slate-600 hover:text-slate-800 flex items-center justify-center gap-1"
                >
                    {showDetails ? 'Hide Details' : 'Show Details'}
                    <ChevronRight className={`w-4 h-4 transition-transform ${showDetails ? 'rotate-90' : ''}`} />
                </button>
            </div>

            {/* Details */}
            {showDetails && (
                <div className="border-t">
                    {/* Factors */}
                    {prediction.factors && prediction.factors.length > 0 && (
                        <div className="px-6 py-4 border-b">
                            <h4 className="font-medium text-slate-700 mb-3">Key Factors</h4>
                            <ul className="space-y-2">
                                {prediction.factors.map((factor, i) => (
                                    <li key={i} className="flex items-start gap-2 text-sm text-slate-600">
                                        <span className="w-1.5 h-1.5 bg-amber-400 rounded-full mt-2 flex-shrink-0" />
                                        {factor}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {/* Similar Cases */}
                    {prediction.similarCases && prediction.similarCases.length > 0 && (
                        <div className="px-6 py-4 border-b bg-slate-50">
                            <h4 className="font-medium text-slate-700 mb-3">Similar Cases</h4>
                            <ul className="space-y-2">
                                {prediction.similarCases.map((case_, i) => (
                                    <li key={i} className="text-sm text-slate-600">
                                        • {case_}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {/* Recommendations */}
                    {prediction.recommendations && prediction.recommendations.length > 0 && (
                        <div className="px-6 py-4 bg-green-50">
                            <h4 className="font-medium text-green-800 mb-3 flex items-center gap-2">
                                <CheckCircle className="w-4 h-4" />
                                Strategic Recommendations
                            </h4>
                            <ul className="space-y-2">
                                {prediction.recommendations.map((rec, i) => (
                                    <li key={i} className="flex items-start gap-2 text-sm text-green-700">
                                        <span className="w-5 h-5 rounded-full bg-green-100 flex items-center justify-center text-xs flex-shrink-0">
                                            {i + 1}
                                        </span>
                                        {rec}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}
                </div>
            )}

            {/* Disclaimer */}
            <div className="px-6 py-3 border-t bg-amber-50 text-xs text-amber-700">
                ⚠️ Predictions are estimates based on available data and should not be relied upon as legal advice.
            </div>
        </div>
    );
}
