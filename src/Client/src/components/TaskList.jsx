// src/components/TaskList.jsx
import React, { useState, useEffect } from 'react';
import axios from 'axios';

const TaskList = () => {
  const [tasks, setTasks] = useState([]); // Store tasks
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const apiUrl = import.meta.env.VITE_API_URL; // API base URL
  const tasksApiPath = import.meta.env.VITE_TASKS_API_PATH; // API path for tasks

  const fetchTasks = async () => {
    try {
      const response = await axios.get(`${apiUrl}${tasksApiPath}`);

      // Sort tasks by ID in descending order (latest tasks first)
      const sortedTasks = response.data.sort((a, b) => b.id - a.id);
      setTasks(sortedTasks);
    } catch (err) {
      setError("Error fetching tasks: " + err.message);
    }
  };

  // Function to create a new task
  const createTask = async () => {
    setLoading(true);
    try {
      const response = await axios.post(`${apiUrl}${tasksApiPath}`);
      setTasks((prevTasks) => [...prevTasks, response.data]); // Add the new task to the list
    } catch (err) {
      setError('Error creating task: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  // Fetch tasks on component mount and set up polling
  useEffect(() => {
    fetchTasks(); // Initial fetch

    // Polling every 5 seconds
    const interval = setInterval(() => {
      fetchTasks();
    }, 5000); // Fetch tasks every 5 seconds

    // Cleanup the interval on unmount
    return () => clearInterval(interval);
  }, [apiUrl, tasksApiPath]);

  return (
    <div>
      <h1>Task Manager</h1>

      {/* Button to create a new task */}
      <button onClick={createTask} disabled={loading}>
        {loading ? 'Creating Task...' : 'Create Task'}
      </button>

      {/* Show error if any */}
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {/* Display the list of tasks */}
      <div>
        <h2>Tasks</h2>
        {tasks.length === 0 ? (
          <p>No tasks available</p>
        ) : (
          <ul>
            {tasks.map((task) => (
              <li key={task.id}>
                <p>Task ID: {task.id}</p>
                <p>Result: {task.result || 'Pending'}</p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};

export default TaskList;
