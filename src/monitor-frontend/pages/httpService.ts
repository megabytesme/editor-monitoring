export const httpRequest = async <T>(options: { method: string; url: string; body?: any }): Promise<T> => {
    try {
      const response = await fetch(options.url, {
        method: options.method,
        headers: { 'Content-Type': 'application/json' },
        body: options.body ? JSON.stringify(options.body) : undefined,
      });
  
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
  
      return response.json();
    } catch (error) {
      console.error('Request failed:', error);
      throw error;
    }
  };
  