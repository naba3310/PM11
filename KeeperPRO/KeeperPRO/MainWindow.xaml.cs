using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace KeeperPRO
{
    public partial class MainWindow : Window
    {
        // Строка подключения к базе данных SQL Server
        private string connectionString = "Server=DESKTOP-M0DT52I;Database=ХранительПРО;Trusted_Connection=True;";

        // ID текущего пользователя (-1 означает, что пользователь не авторизован)
        private int currentUserId = -1;

        public MainWindow()
        {
            InitializeComponent();
            // Устанавливаем первую вкладку (логин) как активную при запуске
            MainTabControl.SelectedIndex = 0;

            // Установка сегодняшней даты по умолчанию для выбора дат в заявке
            StartDatePicker.SelectedDate = DateTime.Now;
            EndDatePicker.SelectedDate = DateTime.Now;
        }

        // Обработчик события нажатия кнопки входа
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для проверки логина и пароля
                    string query = "SELECT user_id FROM users WHERE login = @Login AND password = @Password";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Password", password);

                    object result = command.ExecuteScalar();

                    if (result != null)
                    {
                        // Успешная авторизация
                        currentUserId = Convert.ToInt32(result);
                        // Показываем вкладку с заявками и скрываем вкладку входа
                        RequestsTab.Visibility = Visibility.Visible;
                        LoginTab.Visibility = Visibility.Collapsed;
                        MainTabControl.SelectedItem = RequestsTab;
                        // Загружаем заявки пользователя
                        LoadUserRequests();
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}");
            }
        }

        // Обработчик события нажатия кнопки регистрации
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения всех обязательных полей
            if (string.IsNullOrEmpty(FullNameTextBox.Text) ||
                string.IsNullOrEmpty(PhoneTextBox.Text) ||
                string.IsNullOrEmpty(EmailTextBox.Text) ||
                BirthDatePicker.SelectedDate == null ||
                string.IsNullOrEmpty(PassportSeriesTextBox.Text) ||
                string.IsNullOrEmpty(PassportNumberTextBox.Text) ||
                string.IsNullOrEmpty(UsernameTextBox.Text) ||
                RegisterPasswordBox.Password.Length == 0 ||
                ConfirmPasswordBox.Password.Length == 0)
            {
                MessageBox.Show("Заполните все обязательные поля");
                return;
            }

            // Проверка совпадения паролей
            if (RegisterPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверка существования пользователя с таким же логином или email
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE login = @Login OR email = @Email";
                    SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@Login", UsernameTextBox.Text);
                    checkCommand.Parameters.AddWithValue("@Email", EmailTextBox.Text);
                    int userCount = (int)checkCommand.ExecuteScalar();

                    if (userCount > 0)
                    {
                        MessageBox.Show("Пользователь с таким логином или email уже существует");
                        return;
                    }

                    // SQL-запрос для вставки нового пользователя
                    string insertQuery = @"INSERT INTO users 
                                (full_name, phone, email, birth_date, passport_series, passport_number, login, password)
                                VALUES 
                                (@FullName, @Phone, @Email, @BirthDate, @PassportSeries, @PassportNumber, @Login, @Password);
                                SELECT SCOPE_IDENTITY();";

                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    // Добавление параметров для нового пользователя
                    insertCommand.Parameters.AddWithValue("@FullName", FullNameTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@Phone", PhoneTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@Email", EmailTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@BirthDate", BirthDatePicker.SelectedDate);
                    insertCommand.Parameters.AddWithValue("@PassportSeries", PassportSeriesTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@PassportNumber", PassportNumberTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@Login", UsernameTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@Password", RegisterPasswordBox.Password);

                    // Выполнение запроса и получение ID нового пользователя
                    currentUserId = Convert.ToInt32(insertCommand.ExecuteScalar());

                    MessageBox.Show("Регистрация успешно завершена!");
                    // Возврат на вкладку входа
                    BackToLoginButton_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}");
            }
        }

        // Обработчик события создания новой заявки
        private void NewRequestButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем вкладку создания заявки и скрываем список заявок
            NewRequestTab.Visibility = Visibility.Visible;
            RequestsTab.Visibility = Visibility.Collapsed;
            MainTabControl.SelectedItem = NewRequestTab;

            // Загружаем списки подразделений и сотрудников
            LoadDepartments();
            LoadEmployees();
        }

        // Обработчик события отправки заявки
        private void SubmitRequestButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбранных дат
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите даты начала и окончания");
                return;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала");
                return;
            }

            // Проверка заполнения обязательных полей
            if (string.IsNullOrEmpty(PurposeTextBox.Text))
            {
                MessageBox.Show("Укажите цель посещения");
                return;
            }

            if (DepartmentComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите подразделение");
                return;
            }

            // Для групповой заявки требуется выбор сотрудника
            if (IsGroupCheckBox.IsChecked == true && EmployeeComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Для групповой заявки выберите сопровождающего сотрудника");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для создания новой заявки
                    string query = @"INSERT INTO requests 
                    (user_id, employee_id, department_id, start_date, end_date, purpose, status_id, is_group, notes)
                    VALUES 
                    (@UserId, @EmployeeId, @DepartmentId, @StartDate, @EndDate, @Purpose, 1, @IsGroup, @Notes)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", currentUserId);

                    // Обработка параметра employee_id (может быть NULL)
                    object employeeIdParam = DBNull.Value;
                    if (IsGroupCheckBox.IsChecked == true && EmployeeComboBox.SelectedValue != null)
                    {
                        employeeIdParam = EmployeeComboBox.SelectedValue;
                    }
                    command.Parameters.AddWithValue("@EmployeeId", employeeIdParam);

                    command.Parameters.AddWithValue("@DepartmentId", DepartmentComboBox.SelectedValue);
                    command.Parameters.AddWithValue("@StartDate", StartDatePicker.SelectedDate);
                    command.Parameters.AddWithValue("@EndDate", EndDatePicker.SelectedDate);
                    command.Parameters.AddWithValue("@Purpose", PurposeTextBox.Text);
                    command.Parameters.AddWithValue("@IsGroup", IsGroupCheckBox.IsChecked == true);

                    // Обработка параметра notes (может быть NULL)
                    object notesParam = DBNull.Value;
                    if (!string.IsNullOrEmpty(NotesTextBox.Text))
                    {
                        notesParam = NotesTextBox.Text;
                    }
                    command.Parameters.AddWithValue("@Notes", notesParam);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Заявка успешно создана!");
                        // Очистка формы и возврат к списку заявок
                        CancelRequestButton_Click(sender, e);
                        LoadUserRequests();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания заявки: {ex.Message}");
            }
        }

        // Обработчик события отмены создания заявки
        private void CancelRequestButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем вкладку создания заявки и показываем список заявок
            NewRequestTab.Visibility = Visibility.Collapsed;
            RequestsTab.Visibility = Visibility.Visible;
            MainTabControl.SelectedItem = RequestsTab;

            // Сброс значений формы
            StartDatePicker.SelectedDate = DateTime.Now;
            EndDatePicker.SelectedDate = DateTime.Now;
            PurposeTextBox.Text = "";
            NotesTextBox.Text = "";
            IsGroupCheckBox.IsChecked = false;
            EmployeeComboBox.SelectedIndex = -1;
            DepartmentComboBox.SelectedIndex = -1;
        }

        // Обработчик события выхода из системы
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Сброс ID пользователя
            currentUserId = -1;
            // Скрытие вкладок заявок и показ вкладки входа
            RequestsTab.Visibility = Visibility.Collapsed;
            NewRequestTab.Visibility = Visibility.Collapsed;
            LoginTab.Visibility = Visibility.Visible;
            MainTabControl.SelectedItem = LoginTab;

            // Очистка полей входа
            LoginTextBox.Text = "";
            PasswordBox.Password = "";
        }

        // Обработчик события обновления списка заявок
        private void RefreshRequestsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUserRequests();
        }

        // Метод загрузки заявок пользователя
        private void LoadUserRequests()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для получения заявок пользователя
                    string query = @"SELECT r.request_id, r.start_date, r.end_date, r.purpose, 
                                   rs.name as status_name, r.is_group
                                   FROM requests r
                                   JOIN request_statuses rs ON r.status_id = rs.status_id
                                   WHERE r.user_id = @UserId
                                   ORDER BY r.start_date DESC";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", currentUserId);

                    // Заполнение DataGrid данными о заявках
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    RequestsDataGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        // Метод загрузки списка сотрудников
        private void LoadEmployees()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для получения списка сотрудников
                    string query = "SELECT employee_id, full_name FROM employees";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Привязка данных к комбобоксу
                    EmployeeComboBox.ItemsSource = dt.DefaultView;
                    EmployeeComboBox.DisplayMemberPath = "full_name";
                    EmployeeComboBox.SelectedValuePath = "employee_id";
                    EmployeeComboBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}");
            }
        }

        // Метод загрузки списка подразделений
        private void LoadDepartments()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL-запрос для получения списка подразделений
                    string query = "SELECT department_id, name FROM departments";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Привязка данных к комбобоксу
                    DepartmentComboBox.ItemsSource = dt.DefaultView;
                    DepartmentComboBox.DisplayMemberPath = "name";
                    DepartmentComboBox.SelectedValuePath = "department_id";
                    DepartmentComboBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        // Обработчик события изменения состояния чекбокса групповой заявки
        private void IsGroupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Показываем элементы для выбора сотрудника при групповой заявке
            EmployeeLabel.Visibility = Visibility.Visible;
            EmployeeComboBox.Visibility = Visibility.Visible;
        }

        private void IsGroupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Скрываем элементы для выбора сотрудника при индивидуальной заявке
            EmployeeLabel.Visibility = Visibility.Collapsed;
            EmployeeComboBox.Visibility = Visibility.Collapsed;
            EmployeeComboBox.SelectedIndex = -1;
        }

        // Обработчик события перехода к форме регистрации
        private void ShowRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterTab.Visibility = Visibility.Visible;
            LoginTab.Visibility = Visibility.Collapsed;
            MainTabControl.SelectedItem = RegisterTab;
        }

        // Обработчик события возврата к форме входа
        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterTab.Visibility = Visibility.Collapsed;
            LoginTab.Visibility = Visibility.Visible;
            MainTabControl.SelectedItem = LoginTab;

            // Очистка полей регистрации
            FullNameTextBox.Text = "";
            PhoneTextBox.Text = "";
            EmailTextBox.Text = "";
            BirthDatePicker.SelectedDate = null;
            PassportSeriesTextBox.Text = "";
            PassportNumberTextBox.Text = "";
            UsernameTextBox.Text = "";
            RegisterPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }
    }
}