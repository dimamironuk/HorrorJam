using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TaskManager : MonoBehaviour
{
    [System.Serializable]
    public class Task
    {
        public string id;
        public string description;
        public bool isCompleted;
    }

    [Header("Tasks List")]
    [SerializeField] private List<Task> defaultTasks = new List<Task>();

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI paperTaskText;

    [Header("Colors")]
    [SerializeField] private string activeTaskColorHex = "#1A1A1A";
    [SerializeField] private string completedTaskColorHex = "#7A7A7A";

    [Header("Task Prefixes")]
    [SerializeField] private string activeTaskPrefix = "[  ]";
    [SerializeField] private string completedTaskPrefix = "[V]";

    [Header("Events")]
    public UnityEvent<string> onTaskCompletedEvent;
    public event Action<string> OnTaskCompleted;

    private void Start()
    {
        UpdateTaskUI();
    }

    public void CompleteTask(string taskId)
    {
        for (int i = 0; i < defaultTasks.Count; i++)
        {
            if (defaultTasks[i].id == taskId && !defaultTasks[i].isCompleted)
            {
                var task = defaultTasks[i];
                task.isCompleted = true;
                defaultTasks[i] = task;

                Debug.Log($"[TaskManager] Task completed: {taskId}");

                UpdateTaskUI();

                OnTaskCompleted?.Invoke(taskId);
                onTaskCompletedEvent?.Invoke(taskId);
                break;
            }
        }
    }

    public bool IsTaskCompleted(string taskId)
    {
        var task = defaultTasks.Find(t => t.id == taskId);
        return task != null && task.isCompleted;
    }

    public void AddTask(string id, string description)
    {
        if (defaultTasks.Exists(t => t.id == id)) return;

        defaultTasks.Add(new Task { id = id, description = description, isCompleted = false });
        UpdateTaskUI();
    }

    public void UpdateTaskUI()
    {
        if (paperTaskText == null) return;

        StringBuilder sb = new StringBuilder();

        foreach (var task in defaultTasks)
        {
            if (task.isCompleted)
            {
                sb.AppendLine($"<color={completedTaskColorHex}>{completedTaskPrefix} <s>{task.description}</s></color>");
            }
            else
            {
                sb.AppendLine($"<color={activeTaskColorHex}>{activeTaskPrefix} {task.description}</color>");
            }
        }

        paperTaskText.text = sb.ToString();
    }
}