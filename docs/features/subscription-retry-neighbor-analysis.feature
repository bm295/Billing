Feature: Phân tích subscription theo số lần retry gần nhau
  Để vận hành hệ thống billing có thể nhận biết các cụm subscription có hành vi retry tương tự
  Với vai trò là billing operator
  Tôi muốn tìm nhóm subscription lớn nhất có số lần retry thuộc hai mức liền kề

  Rule: Một retry là PaymentAttempt có AttemptNumber lớn hơn 1

    Scenario: Tìm nhóm lớn nhất từ các mức retry liền kề
      Given số lần retry của các subscription là 1, 1, 2, 2, 2, 4
      When hệ thống phân tích các mức retry gần nhau
      Then khoảng retry được chọn là từ 1 đến 2
      And số subscription trong nhóm là 5

    Scenario: Subscription chưa retry được tính ở mức 0
      Given số lần retry của các subscription là 0, 0, 1, 3
      When hệ thống phân tích các mức retry gần nhau
      Then khoảng retry được chọn là từ 0 đến 1
      And số subscription trong nhóm là 3

    Scenario: Chọn khoảng thấp hơn khi nhiều nhóm có cùng kích thước
      Given số lần retry của các subscription là 0, 1, 3, 4
      When hệ thống phân tích các mức retry gần nhau
      Then khoảng retry được chọn là từ 0 đến 1
      And số subscription trong nhóm là 2

    Scenario: Không giới hạn số retry ở 100
      Given số lần retry của các subscription là 100, 101, 101, 250
      When hệ thống phân tích các mức retry gần nhau
      Then khoảng retry được chọn là từ 100 đến 101
      And số subscription trong nhóm là 3

    Scenario: Chưa có subscription
      Given hệ thống chưa có subscription nào
      When hệ thống phân tích các mức retry gần nhau
      Then số subscription trong nhóm là 0
      And khoảng retry không được xác định

  Rule: Phân tích chỉ đọc dữ liệu thanh toán

    Scenario: Chạy phân tích không tạo payment attempt mới
      Given hệ thống đã có payment và payment attempt
      When billing operator yêu cầu phân tích
      Then số payment attempt không thay đổi
      And trạng thái invoice và payment không thay đổi
