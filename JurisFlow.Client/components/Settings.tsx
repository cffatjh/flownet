import React, { useState, useEffect } from 'react';
import { Settings as SettingsIcon, User, Globe, DollarSign, Bell, Lock, X, Shield, Moon, Sun, Briefcase, CreditCard } from './Icons';
import { useTranslation, Currency } from '../contexts/LanguageContext';
import { useAuth } from '../contexts/AuthContext';
import { useData } from '../contexts/DataContext';
import { useTheme } from '../contexts/ThemeContext';
import { Language } from '../translations';
import { BillingSettings, FirmSettings, SecuritySettings } from '../types';
import AdminPanel from './AdminPanel';
import { toast } from './Toast';

const Settings: React.FC = () => {
  const { t, language, setLanguage, currency, setCurrency } = useTranslation();
  const { user } = useAuth();
  const { updateUserProfile } = useData();
  const { theme, setTheme, resolvedTheme } = useTheme();

  const [activeTab, setActiveTab] = useState<'profile' | 'preferences' | 'notifications' | 'security' | 'firm' | 'billing' | 'admin'>('profile');
  const [profileData, setProfileData] = useState({
    name: '',
    email: '',
    phone: '',
    mobile: '',
    address: '',
    city: '',
    state: '',
    zipCode: '',
    country: '',
    barNumber: '',
    bio: ''
  });
  const [saving, setSaving] = useState(false);

  // Firm Settings State
  const [firmSettings, setFirmSettings] = useState<FirmSettings>({
    firmName: 'Your Law Firm',
    taxId: '',
    ledesFirmId: '',
    address: '',
    city: '',
    state: '',
    zipCode: '',
    phone: '',
    website: ''
  });

  // Billing Settings State
  const [billingSettings, setBillingSettings] = useState<BillingSettings>({
    defaultHourlyRate: 350,
    partnerRate: 500,
    associateRate: 300,
    paralegalRate: 150,
    billingIncrement: 6,
    minimumTimeEntry: 6,
    roundingRule: 'up',
    defaultPaymentTerms: 30,
    invoicePrefix: 'INV-',
    defaultTaxRate: 0,
    ledesEnabled: false,
    utbmsCodesRequired: false,
    evergreenRetainerMinimum: 5000,
    trustBalanceAlerts: true
  });

  // Security Settings State
  const [securitySettings, setSecuritySettings] = useState<SecuritySettings>({
    minPasswordLength: 8,
    requireUppercase: true,
    requireNumbers: true,
    requireSpecialChars: false,
    passwordExpiryDays: 90,
    mfaEnabled: false,
    sessionTimeoutMinutes: 60,
    auditLoggingEnabled: true
  });

  useEffect(() => {
    if (user) {
      setProfileData({
        name: user.name || '',
        email: user.email || '',
        phone: '',
        mobile: '',
        address: '',
        city: '',
        state: '',
        zipCode: '',
        country: '',
        barNumber: '',
        bio: ''
      });
    }
  }, [user]);

  const FLAGS: Partial<Record<Language, string>> = {
    en: 'EN',
    tr: 'TR'
  };

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await updateUserProfile(profileData);
      toast.success('Profile updated successfully');
    } catch (error) {
      toast.error('Failed to update profile');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="h-full flex flex-col bg-gray-50/50">
      <div className="px-8 py-6 border-b border-gray-200 bg-white">
        <div className="flex items-center gap-3 mb-2">
          <SettingsIcon className="w-6 h-6 text-slate-800" />
          <h1 className="text-2xl font-bold text-slate-800">Settings</h1>
        </div>
        <p className="text-sm text-gray-500">Manage your account settings and preferences</p>
      </div>

      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar */}
        <div className="w-64 bg-white border-r border-gray-200 p-4">
          <nav className="space-y-1">
            <button
              onClick={() => setActiveTab('profile')}
              className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${activeTab === 'profile'
                ? 'bg-slate-100 text-slate-900'
                : 'text-gray-600 hover:bg-gray-50'
                }`}
            >
              <User className="w-5 h-5" />
              Profile
            </button>
            <button
              onClick={() => setActiveTab('preferences')}
              className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${activeTab === 'preferences'
                ? 'bg-slate-100 text-slate-900'
                : 'text-gray-600 hover:bg-gray-50'
                }`}
            >
              <Globe className="w-5 h-5" />
              Preferences
            </button>
            <button
              onClick={() => setActiveTab('notifications')}
              className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${activeTab === 'notifications'
                ? 'bg-slate-100 text-slate-900'
                : 'text-gray-600 hover:bg-gray-50'
                }`}
            >
              <Bell className="w-5 h-5" />
              Notifications
            </button>
            <button
              onClick={() => setActiveTab('security')}
              className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${activeTab === 'security'
                ? 'bg-slate-100 text-slate-900'
                : 'text-gray-600 hover:bg-gray-50'
                }`}
            >
              <Lock className="w-5 h-5" />
              Security
            </button>

            {/* Admin-only sections */}
            {user?.role === 'Admin' && (
              <>
                <div className="pt-4 mt-4 border-t border-gray-200">
                  <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider px-4 mb-2">Firm Settings</p>
                </div>
                <button
                  onClick={() => setActiveTab('firm')}
                  className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${activeTab === 'firm'
                    ? 'bg-slate-100 text-slate-900'
                    : 'text-gray-600 hover:bg-gray-50'
                    }`}
                >
                  <Briefcase className="w-5 h-5" />
                  Firm Info
                </button>
                <button
                  onClick={() => setActiveTab('billing')}
                  className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${activeTab === 'billing'
                    ? 'bg-slate-100 text-slate-900'
                    : 'text-gray-600 hover:bg-gray-50'
                    }`}
                >
                  <CreditCard className="w-5 h-5" />
                  Billing
                </button>
                <button
                  onClick={() => setActiveTab('admin')}
                  className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${activeTab === 'admin'
                    ? 'bg-slate-100 text-slate-900'
                    : 'text-gray-600 hover:bg-gray-50'
                    }`}
                >
                  <Shield className="w-5 h-5" />
                  Admin Panel
                </button>
              </>
            )}
          </nav>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-8">
          {activeTab === 'profile' && (
            <div className="max-w-2xl">
              <h2 className="text-xl font-bold text-slate-800 mb-6">Profile Information</h2>
              <form onSubmit={handleSaveProfile} className="space-y-6">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Full Name</label>
                    <input
                      type="text"
                      required
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={profileData.name}
                      onChange={e => setProfileData({ ...profileData, name: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                    <input
                      type="email"
                      required
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={profileData.email}
                      onChange={e => setProfileData({ ...profileData, email: e.target.value })}
                    />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                    <input
                      type="tel"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={profileData.phone}
                      onChange={e => setProfileData({ ...profileData, phone: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Mobile</label>
                    <input
                      type="tel"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={profileData.mobile}
                      onChange={e => setProfileData({ ...profileData, mobile: e.target.value })}
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                  <input
                    type="text"
                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    value={profileData.address}
                    onChange={e => setProfileData({ ...profileData, address: e.target.value })}
                  />
                </div>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">City</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={profileData.city}
                      onChange={e => setProfileData({ ...profileData, city: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">State</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={profileData.state}
                      onChange={e => setProfileData({ ...profileData, state: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">ZIP Code</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={profileData.zipCode}
                      onChange={e => setProfileData({ ...profileData, zipCode: e.target.value })}
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Country</label>
                  <input
                    type="text"
                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    value={profileData.country}
                    onChange={e => setProfileData({ ...profileData, country: e.target.value })}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Bar Number</label>
                  <input
                    type="text"
                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    value={profileData.barNumber}
                    onChange={e => setProfileData({ ...profileData, barNumber: e.target.value })}
                    placeholder="Bar association registration number"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Bio</label>
                  <textarea
                    rows={4}
                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    value={profileData.bio}
                    onChange={e => setProfileData({ ...profileData, bio: e.target.value })}
                    placeholder="Professional bio or description"
                  />
                </div>
                <div className="flex justify-end gap-3 pt-4">
                  <button
                    type="submit"
                    disabled={saving}
                    className="px-6 py-2.5 bg-slate-800 text-white rounded-lg text-sm font-bold hover:bg-slate-900 disabled:opacity-50"
                  >
                    {saving ? 'Saving...' : 'Save Changes'}
                  </button>
                </div>
              </form>
            </div>
          )}

          {activeTab === 'preferences' && (
            <div className="max-w-2xl">
              <h2 className="text-xl font-bold text-slate-800 mb-6">Preferences</h2>
              <div className="space-y-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-3">Language</label>
                  <div className="grid grid-cols-2 gap-3">
                    {Object.entries(FLAGS).map(([lang, flag]) => (
                      <button
                        key={lang}
                        onClick={() => setLanguage(lang as Language)}
                        className={`p-4 border-2 rounded-lg text-center transition-all ${language === lang
                          ? 'border-primary-500 bg-primary-50'
                          : 'border-gray-200 hover:border-gray-300'
                          }`}
                      >
                        <div className="text-2xl mb-2">{flag}</div>
                        <div className="text-xs font-bold uppercase">{lang}</div>
                      </button>
                    ))}
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-3">Currency</label>
                  <div className="grid grid-cols-4 gap-3">
                    {['USD', 'EUR', 'TRY', 'GBP'].map(curr => (
                      <button
                        key={curr}
                        onClick={() => setCurrency(curr as Currency)}
                        className={`p-4 border-2 rounded-lg text-center font-bold transition-all ${currency === curr
                          ? 'border-primary-500 bg-primary-50'
                          : 'border-gray-200 hover:border-gray-300'
                          }`}
                      >
                        {curr}
                      </button>
                    ))}
                  </div>
                </div>

                {/* Theme Selection */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Theme</label>
                  <div className="grid grid-cols-3 gap-3">
                    <button
                      onClick={() => setTheme('light')}
                      className={`p-4 border-2 rounded-lg text-center transition-all ${theme === 'light'
                        ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
                        : 'border-gray-200 dark:border-gray-700 hover:border-gray-300'
                        }`}
                    >
                      <Sun className="w-6 h-6 mx-auto mb-2 text-amber-500" />
                      <div className="text-xs font-bold uppercase">Light</div>
                    </button>
                    <button
                      onClick={() => setTheme('dark')}
                      className={`p-4 border-2 rounded-lg text-center transition-all ${theme === 'dark'
                        ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
                        : 'border-gray-200 dark:border-gray-700 hover:border-gray-300'
                        }`}
                    >
                      <Moon className="w-6 h-6 mx-auto mb-2 text-indigo-500" />
                      <div className="text-xs font-bold uppercase">Dark</div>
                    </button>
                    <button
                      onClick={() => setTheme('system')}
                      className={`p-4 border-2 rounded-lg text-center transition-all ${theme === 'system'
                        ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
                        : 'border-gray-200 dark:border-gray-700 hover:border-gray-300'
                        }`}
                    >
                      <div className="flex justify-center gap-1 mb-2">
                        <Sun className="w-4 h-4 text-amber-500" />
                        <Moon className="w-4 h-4 text-indigo-500" />
                      </div>
                      <div className="text-xs font-bold uppercase">System</div>
                    </button>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                    Currently using: {resolvedTheme} mode
                  </p>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'notifications' && (
            <div className="max-w-2xl">
              <h2 className="text-xl font-bold text-slate-800 mb-6">Notification Preferences</h2>
              <div className="space-y-4">
                <div className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg">
                  <div>
                    <h3 className="font-semibold text-slate-800">Email Notifications</h3>
                    <p className="text-sm text-gray-500">Receive email notifications for important updates</p>
                  </div>
                  <input type="checkbox" defaultChecked className="w-5 h-5" />
                </div>
                <div className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg">
                  <div>
                    <h3 className="font-semibold text-slate-800">Task Reminders</h3>
                    <p className="text-sm text-gray-500">Get reminders for upcoming tasks</p>
                  </div>
                  <input type="checkbox" defaultChecked className="w-5 h-5" />
                </div>
                <div className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg">
                  <div>
                    <h3 className="font-semibold text-slate-800">Calendar Events</h3>
                    <p className="text-sm text-gray-500">Notifications for calendar events</p>
                  </div>
                  <input type="checkbox" defaultChecked className="w-5 h-5" />
                </div>
              </div>
            </div>
          )}

          {activeTab === 'security' && (
            <div className="max-w-2xl">
              <h2 className="text-xl font-bold text-slate-800 mb-6">Security Settings</h2>

              {/* Password Change */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
                <h3 className="font-semibold text-slate-800 mb-4">Change Password</h3>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Current Password</label>
                    <input
                      type="password"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      placeholder="Enter current password"
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
                      <input
                        type="password"
                        className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                        placeholder="Enter new password"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Confirm Password</label>
                      <input
                        type="password"
                        className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                        placeholder="Confirm new password"
                      />
                    </div>
                  </div>
                  <div className="flex justify-end">
                    <button className="px-4 py-2 bg-slate-800 text-white rounded-lg text-sm font-bold hover:bg-slate-900">
                      Update Password
                    </button>
                  </div>
                </div>
              </div>

              {/* Two-Factor Authentication */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="font-semibold text-slate-800">Two-Factor Authentication</h3>
                    <p className="text-sm text-gray-500 mt-1">Add an extra layer of security to your account</p>
                  </div>
                  <button
                    onClick={() => setSecuritySettings({ ...securitySettings, mfaEnabled: !securitySettings.mfaEnabled })}
                    className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${securitySettings.mfaEnabled ? 'bg-green-500' : 'bg-gray-300'}`}
                  >
                    <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${securitySettings.mfaEnabled ? 'translate-x-6' : 'translate-x-1'}`} />
                  </button>
                </div>
                {securitySettings.mfaEnabled && (
                  <div className="mt-4 p-4 bg-green-50 border border-green-200 rounded-lg">
                    <p className="text-sm text-green-800">MFA is enabled. Use an authenticator app for verification.</p>
                  </div>
                )}
              </div>

              {/* Session Settings */}
              <div className="bg-white border border-gray-200 rounded-xl p-6">
                <h3 className="font-semibold text-slate-800 mb-4">Session Settings</h3>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Session Timeout (minutes)</label>
                    <select
                      value={securitySettings.sessionTimeoutMinutes}
                      onChange={(e) => setSecuritySettings({ ...securitySettings, sessionTimeoutMinutes: parseInt(e.target.value) })}
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    >
                      <option value={15}>15 minutes</option>
                      <option value={30}>30 minutes</option>
                      <option value={60}>1 hour</option>
                      <option value={120}>2 hours</option>
                      <option value={480}>8 hours</option>
                    </select>
                  </div>
                  <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                    <div>
                      <h4 className="font-medium text-slate-700">Audit Logging</h4>
                      <p className="text-xs text-gray-500">Track user actions for compliance</p>
                    </div>
                    <button
                      onClick={() => setSecuritySettings({ ...securitySettings, auditLoggingEnabled: !securitySettings.auditLoggingEnabled })}
                      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${securitySettings.auditLoggingEnabled ? 'bg-green-500' : 'bg-gray-300'}`}
                    >
                      <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${securitySettings.auditLoggingEnabled ? 'translate-x-6' : 'translate-x-1'}`} />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Firm Info Tab - Admin Only */}
          {activeTab === 'firm' && (
            <div className="max-w-2xl">
              <h2 className="text-xl font-bold text-slate-800 mb-6">Firm Information</h2>
              <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Firm Name</label>
                  <input
                    type="text"
                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    value={firmSettings.firmName}
                    onChange={(e) => setFirmSettings({ ...firmSettings, firmName: e.target.value })}
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Tax ID (EIN)</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      placeholder="XX-XXXXXXX"
                      value={firmSettings.taxId}
                      onChange={(e) => setFirmSettings({ ...firmSettings, taxId: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">LEDES Firm ID</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      placeholder="For e-billing"
                      value={firmSettings.ledesFirmId}
                      onChange={(e) => setFirmSettings({ ...firmSettings, ledesFirmId: e.target.value })}
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                  <input
                    type="text"
                    className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    value={firmSettings.address}
                    onChange={(e) => setFirmSettings({ ...firmSettings, address: e.target.value })}
                  />
                </div>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">City</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={firmSettings.city}
                      onChange={(e) => setFirmSettings({ ...firmSettings, city: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">State</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={firmSettings.state}
                      onChange={(e) => setFirmSettings({ ...firmSettings, state: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">ZIP Code</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={firmSettings.zipCode}
                      onChange={(e) => setFirmSettings({ ...firmSettings, zipCode: e.target.value })}
                    />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                    <input
                      type="tel"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={firmSettings.phone}
                      onChange={(e) => setFirmSettings({ ...firmSettings, phone: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Website</label>
                    <input
                      type="url"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      placeholder="https://"
                      value={firmSettings.website || ''}
                      onChange={(e) => setFirmSettings({ ...firmSettings, website: e.target.value })}
                    />
                  </div>
                </div>
                <div className="flex justify-end pt-4 border-t">
                  <button
                    onClick={() => toast.success('Firm settings saved')}
                    className="px-6 py-2.5 bg-slate-800 text-white rounded-lg text-sm font-bold hover:bg-slate-900"
                  >
                    Save Changes
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Billing Tab - Admin Only */}
          {activeTab === 'billing' && (
            <div className="max-w-2xl">
              <h2 className="text-xl font-bold text-slate-800 mb-6">Billing & Rates</h2>

              {/* Default Rates */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
                <h3 className="font-semibold text-slate-800 mb-4">Default Hourly Rates</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Partner Rate</label>
                    <div className="relative">
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500">$</span>
                      <input
                        type="number"
                        className="w-full border border-gray-300 rounded-lg p-2.5 pl-7 text-sm"
                        value={billingSettings.partnerRate}
                        onChange={(e) => setBillingSettings({ ...billingSettings, partnerRate: parseInt(e.target.value) || 0 })}
                      />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Associate Rate</label>
                    <div className="relative">
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500">$</span>
                      <input
                        type="number"
                        className="w-full border border-gray-300 rounded-lg p-2.5 pl-7 text-sm"
                        value={billingSettings.associateRate}
                        onChange={(e) => setBillingSettings({ ...billingSettings, associateRate: parseInt(e.target.value) || 0 })}
                      />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Paralegal Rate</label>
                    <div className="relative">
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500">$</span>
                      <input
                        type="number"
                        className="w-full border border-gray-300 rounded-lg p-2.5 pl-7 text-sm"
                        value={billingSettings.paralegalRate}
                        onChange={(e) => setBillingSettings({ ...billingSettings, paralegalRate: parseInt(e.target.value) || 0 })}
                      />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Default Rate</label>
                    <div className="relative">
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500">$</span>
                      <input
                        type="number"
                        className="w-full border border-gray-300 rounded-lg p-2.5 pl-7 text-sm"
                        value={billingSettings.defaultHourlyRate}
                        onChange={(e) => setBillingSettings({ ...billingSettings, defaultHourlyRate: parseInt(e.target.value) || 0 })}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Time Entry Rules */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
                <h3 className="font-semibold text-slate-800 mb-4">Time Entry Rules</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Billing Increment</label>
                    <select
                      value={billingSettings.billingIncrement}
                      onChange={(e) => setBillingSettings({ ...billingSettings, billingIncrement: parseInt(e.target.value) as 6 | 10 | 15 })}
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    >
                      <option value={6}>6 minutes (0.1 hr)</option>
                      <option value={10}>10 minutes</option>
                      <option value={15}>15 minutes (0.25 hr)</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Rounding</label>
                    <select
                      value={billingSettings.roundingRule}
                      onChange={(e) => setBillingSettings({ ...billingSettings, roundingRule: e.target.value as 'up' | 'down' | 'nearest' })}
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    >
                      <option value="up">Round Up</option>
                      <option value="down">Round Down</option>
                      <option value="nearest">Round to Nearest</option>
                    </select>
                  </div>
                </div>
              </div>

              {/* Invoice Settings */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
                <h3 className="font-semibold text-slate-800 mb-4">Invoice Defaults</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Payment Terms</label>
                    <select
                      value={billingSettings.defaultPaymentTerms}
                      onChange={(e) => setBillingSettings({ ...billingSettings, defaultPaymentTerms: parseInt(e.target.value) })}
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                    >
                      <option value={14}>Net 14</option>
                      <option value={30}>Net 30</option>
                      <option value={45}>Net 45</option>
                      <option value={60}>Net 60</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Invoice Prefix</label>
                    <input
                      type="text"
                      className="w-full border border-gray-300 rounded-lg p-2.5 text-sm"
                      value={billingSettings.invoicePrefix}
                      onChange={(e) => setBillingSettings({ ...billingSettings, invoicePrefix: e.target.value })}
                    />
                  </div>
                </div>
              </div>

              {/* LEDES/UTBMS */}
              <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
                <h3 className="font-semibold text-slate-800 mb-4">E-Billing (LEDES)</h3>
                <div className="space-y-4">
                  <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                    <div>
                      <h4 className="font-medium text-slate-700">Enable LEDES Export</h4>
                      <p className="text-xs text-gray-500">Generate LEDES 1998B formatted invoices</p>
                    </div>
                    <button
                      onClick={() => setBillingSettings({ ...billingSettings, ledesEnabled: !billingSettings.ledesEnabled })}
                      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${billingSettings.ledesEnabled ? 'bg-green-500' : 'bg-gray-300'}`}
                    >
                      <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${billingSettings.ledesEnabled ? 'translate-x-6' : 'translate-x-1'}`} />
                    </button>
                  </div>
                  <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                    <div>
                      <h4 className="font-medium text-slate-700">Require UTBMS Codes</h4>
                      <p className="text-xs text-gray-500">Require activity codes on time entries</p>
                    </div>
                    <button
                      onClick={() => setBillingSettings({ ...billingSettings, utbmsCodesRequired: !billingSettings.utbmsCodesRequired })}
                      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${billingSettings.utbmsCodesRequired ? 'bg-green-500' : 'bg-gray-300'}`}
                    >
                      <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${billingSettings.utbmsCodesRequired ? 'translate-x-6' : 'translate-x-1'}`} />
                    </button>
                  </div>
                </div>
              </div>

              {/* Trust Account */}
              <div className="bg-white border border-gray-200 rounded-xl p-6">
                <h3 className="font-semibold text-slate-800 mb-4">Trust Account (IOLTA)</h3>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Evergreen Retainer Minimum</label>
                    <div className="relative">
                      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500">$</span>
                      <input
                        type="number"
                        className="w-full border border-gray-300 rounded-lg p-2.5 pl-7 text-sm"
                        value={billingSettings.evergreenRetainerMinimum}
                        onChange={(e) => setBillingSettings({ ...billingSettings, evergreenRetainerMinimum: parseInt(e.target.value) || 0 })}
                      />
                    </div>
                    <p className="text-xs text-gray-500 mt-1">Alert when client trust balance falls below this amount</p>
                  </div>
                  <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                    <div>
                      <h4 className="font-medium text-slate-700">Trust Balance Alerts</h4>
                      <p className="text-xs text-gray-500">Email notifications for low balances</p>
                    </div>
                    <button
                      onClick={() => setBillingSettings({ ...billingSettings, trustBalanceAlerts: !billingSettings.trustBalanceAlerts })}
                      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${billingSettings.trustBalanceAlerts ? 'bg-green-500' : 'bg-gray-300'}`}
                    >
                      <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${billingSettings.trustBalanceAlerts ? 'translate-x-6' : 'translate-x-1'}`} />
                    </button>
                  </div>
                </div>
                <div className="flex justify-end pt-4 mt-4 border-t">
                  <button
                    onClick={() => toast.success('Billing settings saved')}
                    className="px-6 py-2.5 bg-slate-800 text-white rounded-lg text-sm font-bold hover:bg-slate-900"
                  >
                    Save Changes
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Admin Panel */}
          {activeTab === 'admin' && (
            <AdminPanel />
          )}
        </div>
      </div>
    </div>
  );
};

export default Settings;


