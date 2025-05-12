using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace EmployeeTerminal
{
    public partial class MainWindow : Window
    {
        // Строка подключения к базе данных SQL Server
        private string connectionString = "Server=DESKTOP-M0DT52I;Database=ХранительПРО;Trusted_Connection=True;";

        // ID текущего авторизованного сотрудника (-1 - не авторизован)
        private int currentEmployeeId = -1;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация фильтра по статусам
            StatusFilterComboBox.Items.Add("Все");
            StatusFilterComboBox.Items.Add("Новая");
            StatusFilterComboBox.Items.Add("В обработке");
            StatusFilterComboBox.Items.Add("Одобрена");
            StatusFilterComboBox.Items.Add("Отклонена");
            StatusFilterComboBox.Items.Add("Завершена");
            StatusFilterComboBox.SelectedIndex = 0; // Устанавливаем "Все" по умолчанию
        }

        // Обработчик нажатия кнопки входа
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string employeeCode = EmployeeCodeTextBox.Text;

            // Проверка ввода кода сотрудника
            if (string.IsNullOrEmpty(employeeCode))
            {
                MessageBox.Show("Введите код сотрудника");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Поиск сотрудника по коду
                    string query = "SELECT employee_id FROM employees WHERE employee_code = @EmployeeCode";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@EmployeeCode", employeeCode);

                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        // Успешная авторизация
                        currentEmployeeId = Convert.ToInt32(result);
                        // Переключение панелей
                        LoginPanel.Visibility = Visibility.Collapsed;
                        MainPanel.Visibility = Visibility.Visible;
                        // Загрузка заявок сотрудника
                        LoadRequests();
                    }
                    else
                    {
                        MessageBox.Show("Сотрудник не найден");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при авторизации: " + ex.Message);
            }
        }

        // Загрузка заявок сотрудника
        private void LoadRequests()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Базовый запрос для получения заявок
                    string query = @"SELECT r.request_id, u.full_name as user_name, 
                                    d.name AS department_name, 
                                    FORMAT(r.start_date, 'dd.MM.yyyy') as start_date,
                                    FORMAT(r.end_date, 'dd.MM.yyyy') as end_date,
                                    s.name AS status_name,
                                    r.purpose
                             FROM requests r
                             LEFT JOIN users u ON r.user_id = u.user_id
                             JOIN departments d ON r.department_id = d.department_id
                             JOIN request_statuses s ON r.status_id = s.status_id
                             WHERE r.employee_id = @EmployeeId";

                    // Добавляем фильтр по статусу, если выбран не "Все"
                    if (StatusFilterComboBox.SelectedIndex > 0)
                    {
                        query += " AND s.name = @Status";
                    }

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@EmployeeId", currentEmployeeId);

                    // Добавляем параметр статуса при необходимости
                    if (StatusFilterComboBox.SelectedIndex > 0)
                    {
                        command.Parameters.AddWithValue("@Status", StatusFilterComboBox.SelectedItem.ToString());
                    }

                    // Заполняем DataGrid
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    RequestsDataGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке заявок: " + ex.Message);
            }
        }

        // Обработчик изменения фильтра по статусу
        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обновляем список заявок только если сотрудник авторизован
            if (currentEmployeeId != -1)
            {
                LoadRequests();
            }
        }

        // Обработчик кнопки проверки заявки
        private void CheckRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestsDataGrid.SelectedItem is DataRowView selectedRow)
            {
                // Получаем ID выбранной заявки
                int requestId = (int)selectedRow["request_id"];
                // Открываем окно проверки заявки
                var checkRequestWindow = new CheckRequestWindow(requestId, connectionString);
                checkRequestWindow.ShowDialog();
                // Обновляем список заявок после закрытия окна проверки
                LoadRequests();
            }
            else
            {
                MessageBox.Show("Выберите заявку для проверки");
            }
        }

        // Обработчик выхода из системы
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем ID сотрудника
            currentEmployeeId = -1;
            // Очищаем поле ввода кода
            EmployeeCodeTextBox.Clear();
            // Переключаем панели
            LoginPanel.Visibility = Visibility.Visible;
            MainPanel.Visibility = Visibility.Collapsed;
        }
    }
}