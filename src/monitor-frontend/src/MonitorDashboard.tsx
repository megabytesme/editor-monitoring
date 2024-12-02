import React, { useState, useEffect } from 'react';
import { httpRequest } from './httpService';

interface EndpointConfig {
    id: number;
    friendlyName: string;
    url: string;
}

interface StatusResult {
    id: number;
    endpoint: string;
    response: string;
    status: number;
    timestamp: string;
}

interface SettingsConfig {
    checkInterval: number;
}

const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
};

const MonitorDashboard: React.FC = () => {
    const [endpoints, setEndpoints] = useState<EndpointConfig[]>([]);
    const [results, setResults] = useState<StatusResult[]>([]);
    const [newEndpoint, setNewEndpoint] = useState<EndpointConfig>({ id: 0, friendlyName: '', url: '' });
    const [settings, setSettings] = useState<SettingsConfig>({ checkInterval: 30 });

    const fetchEndpoints = async () => {
        try {
            const response = await httpRequest<EndpointConfig[]>({
                method: 'GET',
                url: 'http://localhost:5000/api/config/endpoints',
            });
            setEndpoints(response);
        } catch (error) {
            console.error('Error fetching endpoints:', error);
        }
    };

    const fetchStatusResults = async () => {
        try {
            const response = await httpRequest<StatusResult[]>({
                method: 'GET',
                url: 'http://localhost:5000/api/status/results',
            });
            setResults(response);
        } catch (error) {
            console.error('Error fetching status results:', error);
        }
    };

    const handleRemoveEndpoint = async (id: number) => {
        try {
            await httpRequest<void>({
                method: 'DELETE',
                url: `http://localhost:5000/api/config/endpoints/${id}`,
            });
            setEndpoints(endpoints.filter((endpoint) => endpoint.id !== id));
            fetchStatusResults();
        } catch (error) {
            console.error('Error removing endpoint:', error);
        }
    };

    const handleAddEndpoint = async () => {
        try {
            const response = await httpRequest<EndpointConfig>({
                method: 'POST',
                url: 'http://localhost:5000/api/config/endpoints',
                body: newEndpoint,
            });
            setEndpoints([...endpoints, response]);
            setNewEndpoint({ id: 0, friendlyName: '', url: '' });
            fetchStatusResults();
        } catch (error) {
            console.error('Error adding endpoint:', error);
        }
    };

    const handleCheckIntervalChange = async (newInterval: number) => {
        try {
            const response = await httpRequest<SettingsConfig>({
                method: 'PUT',
                url: 'http://localhost:5000/api/config/settings',
                body: { id: 1, checkInterval: newInterval },
            });
            setSettings(response);
        } catch (error) {
            console.error('Error updating check interval:', error);
        }
    };

    useEffect(() => {
        fetchEndpoints();
        fetchStatusResults();
    }, []);

    return (
        <div>
            <h1>Monitoring Dashboard</h1>

            {endpoints.map((endpoint) => {
                const result = results.find(result => result.endpoint === endpoint.url);
                return (
                    <div key={endpoint.id} className="endpoint-box">
                        <h3>{endpoint.friendlyName}</h3>
                        <p>URL: {endpoint.url}</p>
                        <p>Status: {result ? result.status : 'Unknown'}</p>
                        <p>Response: {result ? result.response : 'Unknown'}</p>
                        <p>Last Checked: {result ? formatTimestamp(result.timestamp) : 'Never'}</p>
                        <button onClick={() => handleRemoveEndpoint(endpoint.id)}>Remove</button>
                    </div>
                );
            })}

            <h2>Add Endpoint</h2>
            <input
                type="text"
                placeholder="Friendly Name"
                value={newEndpoint.friendlyName}
                onChange={(e) => setNewEndpoint({ ...newEndpoint, friendlyName: e.target.value })}
            />
            <input
                type="text"
                placeholder="URL"
                value={newEndpoint.url}
                onChange={(e) => setNewEndpoint({ ...newEndpoint, url: e.target.value })}
            />
            <button onClick={handleAddEndpoint}>Add</button>

            <h2>Settings</h2>
            <label>
                Check Interval (seconds):
                <input
                    type="number"
                    value={settings.checkInterval}
                    onChange={(e) => handleCheckIntervalChange(Number(e.target.value))}
                />
            </label>
        </div>
    );
};

export default MonitorDashboard;
