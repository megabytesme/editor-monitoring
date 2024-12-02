import React, { useState, useEffect } from 'react';
import { httpRequest } from './httpService';

interface TestResult {
    id: number;
    endpoint: string;
    response: string;
    isValid: boolean;
    timestamp: string;
}

const MonitorDashboard: React.FC = () => {
    const [endpoint, setEndpoint] = useState('');
    const [results, setResults] = useState<TestResult[]>([]);

    const handleTestEndpoint = async () => {
        try {
            const response = await httpRequest<{ message: string }>({
                method: 'POST',
                url: 'http://localhost:5000/api/EndpointTester/test-endpoint',
                body: { endpoint },
            });
            console.log(response.message);
            fetchResults();
        } catch (error) {
            console.error('Error testing endpoint:', error);
        }
    };

    const fetchResults = async () => {
        try {
            const response = await httpRequest<TestResult[]>({
                method: 'GET',
                url: 'http://localhost:5000/api/EndpointTester/results',
            });
            setResults(response);
        } catch (error) {
            console.error('Error fetching results:', error);
        }
    };

    useEffect(() => {
        fetchResults();
    }, []);

    return (
        <div>
            <h1>Monitoring Dashboard</h1>
            <input
                type="text"
                value={endpoint}
                onChange={(e) => setEndpoint(e.target.value)}
                placeholder="Enter endpoint"
            />
            <button onClick={handleTestEndpoint}>Test Endpoint</button>

            <h2>Results</h2>
            <ul>
                {results.map((result) => (
                    <li key={result.id}>
                        {result.timestamp}: {result.endpoint} - {result.response} - {result.isValid ? 'Valid' : 'Invalid'}
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default MonitorDashboard;
