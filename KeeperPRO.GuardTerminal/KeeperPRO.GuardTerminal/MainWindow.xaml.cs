using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KeeperPRO.GuardTerminal
{
    public partial class MainWindow : Window
    {
        // Строка подключения к базе данных
        private readonly string connectionString;
        // ID текущего авторизованного сотрудника
        private int currentEmployeeId;

        public MainWindow()
        {
            InitializeComponent();
            // Получаем строку подключения из конфигурации
            connectionString = ConfigurationManager.ConnectionStrings["KeeperPROConnection"]?.ConnectionString;

            // Проверка наличия строки подключения
            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Ошибка подключения к базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // Загрузка данных при старте приложения
            LoadDepartments();
            LoadStatuses();

            // Настройка текста подсказки в поле поиска
            SearchBox.Text = "Поиск по ФИО или паспорту";
            SearchBox.Foreground = Brushes.Gray;
        }

        // Обработчик получения фокуса полем поиска
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Поиск по ФИО или паспорту")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = SystemColors.WindowTextBrush;
            }
        }

        // Обработчик потери фокуса полем поиска
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Поиск по ФИО или паспорту";
                SearchBox.Foreground = Brushes.Gray;
            }
        }

        // Обработчик входа сотрудника
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string code = EmployeeCodeBox.Password.Trim();

            // Проверка ввода кода
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Введите код сотрудника", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Поиск сотрудника по коду
                    var command = new SqlCommand(
                        "SELECT employee_id FROM employees WHERE employee_code = @code",
                        connection);
                    command.Parameters.AddWithValue("@code", code);

                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        // Успешная авторизация
                        currentEmployeeId = Convert.ToInt32(result);
                        // Переключение интерфейса
                        AuthPanel.Visibility = Visibility.Collapsed;
                        MainPanel.Visibility = Visibility.Visible;
                        // Загрузка списка заявок
                        LoadRequests();
                    }
                    else
                    {
                        MessageBox.Show("Неверный код сотрудника", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка списка заявок
        private void LoadRequests()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Запрос для получения одобренных заявок
                    var command = new SqlCommand(
                        @"SELECT r.request_id, r.visit_date, 
                         u.full_name as visitor_name, 
                         u.passport_series + ' ' + u.passport_number as passport,
                         d.name as department_name, r.is_group, rs.name as status_name,
                         r.processed_date, r.status_id, r.access_granted, r.purpose
                         FROM requests r
                         JOIN users u ON r.user_id = u.user_id
                         JOIN departments d ON r.department_id = d.department_id
                         JOIN request_statuses rs ON r.status_id = rs.status_id
                         WHERE r.status_id = 3 -- Одобренные заявки
                         ORDER BY r.visit_date DESC",
                        connection);

                    // Заполнение DataGrid
                    var adapter = new SqlDataAdapter(command);
                    var table = new DataTable();
                    adapter.Fill(table);

                    RequestsGrid.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка списка подразделений
        private void LoadDepartments()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT department_id, name FROM departments", connection);
                    var adapter = new SqlDataAdapter(command);
                    var table = new DataTable();
                    adapter.Fill(table);

                    DepartmentFilter.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка списка статусов
        private void LoadStatuses()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Загрузка только нужных статусов (3,4,5)
                    var command = new SqlCommand("SELECT status_id, name FROM request_statuses WHERE status_id IN (3, 4, 5)", connection);
                    var adapter = new SqlDataAdapter(command);
                    var table = new DataTable();
                    adapter.Fill(table);

                    StatusCombo.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статусов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик двойного клика по заявке
        private void RequestsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = RequestsGrid.SelectedItem as DataRowView;
            if (row == null) return;

            // Заполнение модального окна данными о заявке
            VisitorName.Text = row["visitor_name"].ToString();
            PassportInfo.Text = row["passport"].ToString();
            VisitDate.Text = ((DateTime)row["visit_date"]).ToString("dd.MM.yyyy HH:mm");
            Department.Text = row["department_name"].ToString();
            Purpose.Text = row["purpose"].ToString();
            StatusCombo.SelectedValue = row["status_id"];
            AccessCheckBox.IsChecked = row["access_granted"] != DBNull.Value && (bool)row["access_granted"];

            // Показ модального окна
            ModalOverlay.Visibility = Visibility.Visible;
        }

        // Сохранение изменений в заявке
        private void SaveRequest_Click(object sender, RoutedEventArgs e)
        {
            var row = RequestsGrid.SelectedItem as DataRowView;
            if (row == null) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Обновление данных заявки
                    var command = new SqlCommand(
                        @"UPDATE requests 
                         SET status_id = @status_id, 
                             access_granted = @access_granted,
                             processed_by = @employee_id,
                             processed_date = GETDATE()
                         WHERE request_id = @request_id",
                        connection);

                    // Параметры для обновления
                    command.Parameters.AddWithValue("@status_id", StatusCombo.SelectedValue);
                    command.Parameters.AddWithValue("@access_granted", AccessCheckBox.IsChecked ?? false);
                    command.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                    command.Parameters.AddWithValue("@request_id", row["request_id"]);

                    if (command.ExecuteNonQuery() > 0)
                    {
                        MessageBox.Show("Изменения сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        // Обновление списка заявок и закрытие модального окна
                        LoadRequests();
                        CloseModal_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Закрытие модального окна
        private void CloseModal_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        // Выход из системы
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            currentEmployeeId = 0;
            MainPanel.Visibility = Visibility.Collapsed;
            AuthPanel.Visibility = Visibility.Visible;
            EmployeeCodeBox.Password = "";
        }

        // Поиск заявок
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText)) return;

            try
            {
                if (RequestsGrid.ItemsSource is DataView view)
                {
                    if (searchText == "Поиск по ФИО или паспорту")
                    {
                        view.RowFilter = "";
                    }
                    else
                    {
                        // Фильтрация по ФИО или паспорту
                        view.RowFilter = $"visitor_name LIKE '%{searchText}%' OR passport LIKE '%{searchText.Replace(" ", "")}%'";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обновление списка заявок
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
            // Сброс поля поиска
            SearchBox.Text = "Поиск по ФИО или паспорту";
            SearchBox.Foreground = Brushes.Gray;
        }
    }
}