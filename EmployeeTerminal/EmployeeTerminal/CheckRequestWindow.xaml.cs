using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace EmployeeTerminal
{
    public partial class CheckRequestWindow : Window
    {
        // ID заявки, которую просматриваем
        private int requestId;
        // Строка подключения к базе данных
        private string connectionString;

        // Конструктор окна проверки заявки
        public CheckRequestWindow(int requestId, string connectionString)
        {
            InitializeComponent();
            this.requestId = requestId;
            this.connectionString = connectionString;
            // Загружаем детали заявки при открытии окна
            LoadRequestDetails();
        }

        // Метод загрузки деталей заявки из базы данных
        private void LoadRequestDetails()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // SQL-запрос для получения полной информации о заявке
                    string query = @"SELECT r.request_id, u.full_name as user_name, u.phone, u.email,
                                     d.name AS department_name, 
                                     FORMAT(r.start_date, 'dd.MM.yyyy') as start_date,
                                     FORMAT(r.end_date, 'dd.MM.yyyy') as end_date,
                                     s.name AS status_name, r.purpose, r.notes,
                                     CASE WHEN r.is_group = 1 THEN 'Да' ELSE 'Нет' END as is_group,
                                     FORMAT(r.visit_date, 'dd.MM.yyyy HH:mm') as visit_date
                              FROM requests r
                              LEFT JOIN users u ON r.user_id = u.user_id
                              JOIN departments d ON r.department_id = d.department_id
                              JOIN request_statuses s ON r.status_id = s.status_id
                              WHERE r.request_id = @RequestId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@RequestId", requestId);

                    // Чтение данных из базы
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        // Заполняем элементы интерфейса полученными данными
                        RequestIdTextBlock.Text = reader["request_id"].ToString();
                        UserNameTextBlock.Text = reader["user_name"].ToString();
                        PhoneTextBlock.Text = reader["phone"].ToString();
                        EmailTextBlock.Text = reader["email"].ToString();
                        DepartmentTextBlock.Text = reader["department_name"].ToString();
                        DatesTextBlock.Text = $"{reader["start_date"]} - {reader["end_date"]}";
                        StatusTextBlock.Text = reader["status_name"].ToString();
                        PurposeTextBlock.Text = reader["purpose"].ToString();
                        NotesTextBlock.Text = reader["notes"].ToString();
                        IsGroupTextBlock.Text = reader["is_group"].ToString();
                        VisitDateTextBlock.Text = reader["visit_date"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке деталей заявки: " + ex.Message);
            }
        }

        // Обработчик кнопки "Одобрить"
        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateRequestStatus("Одобрена");
        }

        // Обработчик кнопки "Отклонить"
        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateRequestStatus("Отклонена");
        }

        // Обработчик кнопки "Завершить"
        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateRequestStatus("Завершена");
        }

        // Метод обновления статуса заявки
        private void UpdateRequestStatus(string status)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // SQL-запрос для обновления статуса заявки
                    string query = @"UPDATE requests 
                               SET status_id = (SELECT status_id FROM request_statuses WHERE name = @Status)
                               WHERE request_id = @RequestId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@RequestId", requestId);
                    command.ExecuteNonQuery();

                    MessageBox.Show($"Статус заявки изменен на '{status}'");
                    // Закрываем окно после обновления статуса
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении статуса: " + ex.Message);
            }
        }
    }
}