const API_BASE_URL = 'https://localhost:7135/api';
let currentPage = 1;
let pageSize = 5;
let totalCount = 0;
let currentFilters = {
    keyword: '',
    categoryId: '',
    tagId: '',
    sortBy: 'CreatedDate',
    sortOrder: 'desc'
};

$(document).ready(function () {
    console.log('Page loaded, initializing...');
    loadNews();
    loadCategories();
    loadTags();
    checkLoginStatus();

    $('#btnSearch').click(function () {
        console.log('Search button clicked');
        currentPage = 1;
        applyFilters();
    });

    $('#btnReset').click(function () {
        console.log('Reset button clicked');
        $('#searchKeyword').val('');
        $('#filterCategory').val('');
        $('#filterTag').val('');
        currentPage = 1;
        currentFilters = {
            keyword: '',
            categoryId: '',
            tagId: '',
            sortBy: 'CreatedDate',
            sortOrder: 'desc'
        };
        loadNews();
    });

    $('#searchKeyword').on('keypress', function (e) {
        if (e.which === 13) {
            e.preventDefault();
            currentPage = 1;
            applyFilters();
        }
    });
});

// Load news using OData
function loadNews() {
    console.log('Loading news...');
    showLoading(true);

    // Build OData query
    let odataQuery = buildODataQuery();
    let url = `${API_BASE_URL}/ActiveNews${odataQuery}`;

    console.log('Request URL:', url);

    $.ajax({
        url: url,
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        success: function (data) {
            console.log('=== SUCCESS RESPONSE ===');
            console.log('Full response:', data);
            console.log('Response type:', typeof data);
            console.log('Is array:', Array.isArray(data));
            console.log('Has value property:', 'value' in data);
            console.log('Has @odata.count:', '@odata.count' in data);

            // Handle different response structures
            let newsData;
            if (data.value) {
                newsData = data.value;
                totalCount = data['@odata.count'] || data.value.length;
                console.log('Using data.value, count:', totalCount);
            } else if (Array.isArray(data)) {
                newsData = data;
                totalCount = data.length;
                console.log('Using data directly (array), count:', totalCount);
            } else {
                newsData = [data];
                totalCount = 1;
                console.log('Using data as single item');
            }

            console.log('News data to display:', newsData);
            console.log('Number of news items:', newsData.length);

            // Apply client-side tag filtering if needed
            if (currentFilters.tagId) {
                console.log('Applying client-side tag filter:', currentFilters.tagId);
                newsData = newsData.filter(news => {
                    const tags = getProperty(news, 'tags') || news.Tags || [];
                    return tags.some(tag => {
                        const tagId = getProperty(tag, 'tagId') || tag.TagId;
                        return tagId === currentFilters.tagId;
                    });
                });
                console.log('After tag filter:', newsData.length);
                totalCount = newsData.length; // Update count after filtering
            }

            displayNews(newsData);
            displayPagination();
            showLoading(false);
        },
        error: function (xhr, status, error) {
            console.error('=== ERROR RESPONSE ===');
            console.error('Status:', status);
            console.error('Error:', error);
            console.error('XHR:', xhr);
            console.error('Response Text:', xhr.responseText);
            console.error('Status Code:', xhr.status);

            showLoading(false);

            let errorMessage = 'Failed to load news. Please try again later.';
            if (xhr.status === 0) {
                errorMessage = 'Cannot connect to server. Please check if the API is running and CORS is configured.';
            } else if (xhr.status === 404) {
                errorMessage = 'API endpoint not found. Please check the API URL.';
            } else if (xhr.status === 500) {
                errorMessage = 'Server error. Please check the server logs.';
            }

            $('#newsList').html(`
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-triangle"></i> ${errorMessage}
                    <br><small>Status: ${xhr.status} - ${error}</small>
                </div>
            `);
        }
    });
}

// Build OData query string
function buildODataQuery() {
    let queryParams = [];
    let filterConditions = [];

    // Always include count
    queryParams.push('$count=true');

    // Pagination using OData $top and $skip
    let skip = (currentPage - 1) * pageSize;
    queryParams.push(`$top=${pageSize}`);
    queryParams.push(`$skip=${skip}`);

    // Build filter conditions
    if (currentFilters.keyword) {
        let keyword = encodeURIComponent(currentFilters.keyword.toLowerCase());
        let searchCondition = `(contains(tolower(NewsTitle),'${keyword}') or contains(tolower(Headline),'${keyword}') or contains(tolower(NewsContent),'${keyword}'))`;
        filterConditions.push(searchCondition);
    }

    if (currentFilters.categoryId) {
        filterConditions.push(`CategoryId eq '${currentFilters.categoryId}'`);
    }

    // Fixed tag filtering - simplified approach
    // Note: If backend doesn't support Tags/any(), we'll handle this client-side
    // or remove tag filter from OData query
    if (currentFilters.tagId) {
        // Try simpler syntax first
        // filterConditions.push(`Tags/any(t: t/TagId eq '${currentFilters.tagId}')`);
        // Skip tag filtering in OData for now - will filter client-side
        console.log('Tag filter will be applied client-side');
    }

    // Always filter active news
    filterConditions.push(`NewsStatus eq true`);

    // Combine all filter conditions
    if (filterConditions.length > 0) {
        queryParams.push(`$filter=${filterConditions.join(' and ')}`);
    }

    // Sorting
    let orderBy = `${currentFilters.sortBy} ${currentFilters.sortOrder}`;
    queryParams.push(`$orderby=${orderBy}`);

    // Expand related data
    queryParams.push('$expand=Category,CreatedBy,UpdatedBy,Tags');

    let query = '?' + queryParams.join('&');
    console.log('Built OData query:', query);
    return query;
}

// Apply filters and reload
function applyFilters() {
    currentFilters.keyword = $('#searchKeyword').val().trim();
    currentFilters.categoryId = $('#filterCategory').val();
    currentFilters.tagId = $('#filterTag').val();
    console.log('Applying filters:', currentFilters);
    currentPage = 1;
    loadNews();
}

// Load categories
function loadCategories() {
    console.log('Loading categories...');
    $.ajax({
        url: `${API_BASE_URL}/activateCategory`,
        method: 'GET',
        success: function (data) {
            console.log('Categories loaded:', data);
            const select = $('#filterCategory');
            let categories = data.value || data;
            categories.forEach(cat => {
                select.append(`<option value="${cat.categoryId}">${cat.categoryName}</option>`);
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading categories:', error);
        }
    });
}

// Load tags
function loadTags() {
    console.log('Loading tags...');
    $.ajax({
        url: `${API_BASE_URL}/Tag?$orderby=TagName`,
        method: 'GET',
        success: function (data) {
            console.log('Tags loaded:', data);
            const select = $('#filterTag');
            let tags = data.value || data;

            const uniqueTags = [];
            const tagMap = new Map();
            tags.forEach(tag => {
                if (!tagMap.has(tag.TagName)) {
                    tagMap.set(tag.TagName, tag.TagId);
                    uniqueTags.push(tag);
                }
            });

            uniqueTags.forEach(tag => {
                select.append(`<option value="${tag.TagId}">${tag.TagName}</option>`);
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading tags:', error);
        }
    });
}

// Helper function to get property value (handles both camelCase and PascalCase)
function getProperty(obj, propName) {
    // Try camelCase first
    if (obj[propName] !== undefined) return obj[propName];
    // Try PascalCase
    const pascalCase = propName.charAt(0).toUpperCase() + propName.slice(1);
    if (obj[pascalCase] !== undefined) return obj[pascalCase];
    return null;
}

// Display news
function displayNews(newsData) {
    console.log('Displaying news, count:', newsData.length);

    if (!newsData || newsData.length === 0) {
        $('#newsList').html(`
            <div class="alert alert-info">
                <i class="fas fa-info-circle"></i> No news found matching your criteria.
            </div>
        `);
        return;
    }

    let html = '';
    newsData.forEach((news, index) => {
        console.log(`Processing news ${index + 1}:`, news);
        console.log('News object keys:', Object.keys(news));

        // Get properties with fallback for both naming conventions
        const newsId = getProperty(news, 'newsArticleId') || news.NewsArticleId || '';
        const newsTitle = getProperty(news, 'newsTitle') || news.NewsTitle || 'Untitled';
        const newsContent = getProperty(news, 'newsContent') || news.NewsContent || '';
        const createdDate = getProperty(news, 'createdDate') || news.CreatedDate || new Date();
        const category = getProperty(news, 'category') || news.Category;
        const tags = getProperty(news, 'tags') || news.Tags || [];

        console.log('Extracted values:', {
            newsId,
            newsTitle,
            hasContent: !!newsContent,
            createdDate,
            hasCategory: !!category,
            tagsCount: tags.length
        });

        const date = new Date(createdDate).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });

        const shortContent = newsContent && newsContent.length > 250
            ? newsContent.substring(0, 250) + '...'
            : (newsContent || '');

        const categoryName = category
            ? (getProperty(category, 'categoryName') || category.CategoryName || 'Uncategorized')
            : 'Uncategorized';

        let tagsHtml = '';
        if (tags && tags.length > 0) {
            tagsHtml = tags.map(tag => {
                const tagName = getProperty(tag, 'tagName') || tag.TagName || 'Tag';
                return `<span class="badge-tag"><i class="fas fa-tag"></i> ${tagName}</span>`;
            }).join('');
        }

        html += `
            <div class="news-card">
                <h2 class="news-title" onclick="showNewsDetail('${newsId}')">
                    ${escapeHtml(newsTitle)}
                </h2>
                <div class="news-meta">
                    <span><i class="far fa-calendar-alt"></i> ${date}</span>
                    <span class="badge-category"><i class="fas fa-folder"></i> ${escapeHtml(categoryName)}</span>
                </div>
                <p class="news-content">${escapeHtml(shortContent)}</p>
                <div class="mt-2">
                    ${tagsHtml}
                </div>
                <button class="btn btn-outline-primary mt-3" onclick="showNewsDetail('${newsId}')">
                    Read More <i class="fas fa-arrow-right"></i>
                </button>
            </div>
        `;
    });

    console.log('Setting HTML to newsList');
    $('#newsList').html(html);
}

// Display pagination
function displayPagination() {
    let totalPages = Math.ceil(totalCount / pageSize);
    if (totalPages === 0) totalPages = 1;

    console.log(`Pagination: Page ${currentPage} of ${totalPages}`);

    if (totalPages <= 1) {
        $('#pagination').html('');
        return;
    }

    let html = '';

    html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="changePage(${currentPage - 1}); return false;">
                    <i class="fas fa-chevron-left"></i> Previous
                </a>
            </li>`;

    for (let i = 1; i <= totalPages; i++) {
        if (i === 1 || i === totalPages || (i >= currentPage - 2 && i <= currentPage + 2)) {
            html += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                        <a class="page-link" href="#" onclick="changePage(${i}); return false;">${i}</a>
                    </li>`;
        } else if (i === currentPage - 3 || i === currentPage + 3) {
            html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
    }

    html += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="changePage(${currentPage + 1}); return false;">
                    Next <i class="fas fa-chevron-right"></i>
                </a>
            </li>`;

    $('#pagination').html(html);
}

// Change page
function changePage(page) {
    let totalPages = Math.ceil(totalCount / pageSize);
    if (page >= 1 && page <= totalPages) {
        currentPage = page;
        loadNews();
        $('html, body').animate({ scrollTop: 400 }, 'smooth');
    }
}

// Show news detail
function showNewsDetail(newsId) {
    console.log('Loading detail for news:', newsId);
    let url = `${API_BASE_URL}/ActiveNews('${newsId}')?$expand=Category,Tags`;

    $.ajax({
        url: url,
        method: 'GET',
        success: function (news) {
            console.log('News detail loaded:', news);
            displayNewsDetail(news);
            loadRelatedNews(news);
            $('#newsDetailModal').modal('show');
        },
        error: function (xhr, status, error) {
            console.error('Error loading news detail:', error);
            alert('Failed to load news details. Please try again.');
        }
    });
}

// Display news detail
function displayNewsDetail(news) {
    const newsTitle = getProperty(news, 'newsTitle') || news.NewsTitle || 'Untitled';
    const newsContent = getProperty(news, 'newsContent') || news.NewsContent || '';
    const createdDate = getProperty(news, 'createdDate') || news.CreatedDate || new Date();
    const category = getProperty(news, 'category') || news.Category;
    const tags = getProperty(news, 'tags') || news.Tags || [];
    const newsSource = getProperty(news, 'newsSource') || news.NewsSource;

    $('#newsDetailTitle').html(`<i class="fas fa-newspaper"></i> ${escapeHtml(newsTitle)}`);

    const date = new Date(createdDate).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });

    const categoryName = category
        ? (getProperty(category, 'categoryName') || category.CategoryName || 'Uncategorized')
        : 'Uncategorized';

    let tagsHtml = '';
    if (tags && tags.length > 0) {
        tagsHtml = tags.map(tag => {
            const tagName = getProperty(tag, 'tagName') || tag.TagName || 'Tag';
            return `<span class="badge-tag"><i class="fas fa-tag"></i> ${tagName}</span>`;
        }).join('');
    }

    let detailHtml = `
        <div class="news-meta mb-3">
            <span><i class="far fa-calendar-alt"></i> ${date}</span>
            <span class="badge-category ml-2"><i class="fas fa-folder"></i> ${escapeHtml(categoryName)}</span>
        </div>
        <div class="mb-3">
            ${tagsHtml}
        </div>
        <div class="news-content">
            ${escapeHtml(newsContent).replace(/\r\n/g, '<br><br>')}
        </div>
    `;

    if (newsSource && newsSource !== 'N/A') {
        detailHtml += `<div class="mt-3"><strong><i class="fas fa-link"></i> Source:</strong> ${escapeHtml(newsSource)}</div>`;
    }

    $('#newsDetailContent').html(detailHtml);
}

// Load related news - FIXED version without Tags/any()
function loadRelatedNews(currentNews) {
    console.log('Loading related news for:', currentNews);

    const categoryId = getProperty(currentNews, 'categoryId') || currentNews.CategoryId;
    const tags = getProperty(currentNews, 'tags') || currentNews.Tags || [];
    const newsArticleId = getProperty(currentNews, 'newsArticleId') || currentNews.NewsArticleId;

    let filterConditions = [];

    // Same category filter only (remove Tags filter from OData)
    if (categoryId) {
        filterConditions.push(`CategoryId eq '${categoryId}'`);
    }

    let filterQuery = '';
    if (filterConditions.length > 0) {
        filterQuery = `(${filterConditions.join(' or ')}) and NewsArticleId ne '${newsArticleId}' and NewsStatus eq true`;
    } else {
        filterQuery = `NewsArticleId ne '${newsArticleId}' and NewsStatus eq true`;
    }

    // Get more items to filter client-side
    let url = `${API_BASE_URL}/ActiveNews?$filter=${encodeURIComponent(filterQuery)}&$top=10&$orderby=CreatedDate desc&$expand=Category,Tags`;

    console.log('Related news URL:', url);

    $.ajax({
        url: url,
        method: 'GET',
        success: function (data) {
            let related = data.value || data;
            console.log('Related news found (before tag filter):', related.length);

            // Client-side filtering by tags
            if (tags && tags.length > 0) {
                const currentTagIds = tags.map(t => getProperty(t, 'tagId') || t.TagId);
                related = related.filter(news => {
                    const newsTags = getProperty(news, 'tags') || news.Tags || [];
                    return newsTags.some(tag => {
                        const tagId = getProperty(tag, 'tagId') || tag.TagId;
                        return currentTagIds.includes(tagId);
                    });
                });
                console.log('Related news found (after tag filter):', related.length);
            }

            // Take only top 3
            related = related.slice(0, 3);

            displayRelatedNews(related);
        },
        error: function (xhr, status, error) {
            console.error('Error loading related news:', error);
            $('#relatedNewsSection').hide();
        }
    });
}

// Display related news
function displayRelatedNews(relatedNews) {
    if (relatedNews.length > 0) {
        let html = '';
        relatedNews.forEach(news => {
            const newsId = getProperty(news, 'newsArticleId') || news.NewsArticleId || '';
            const newsTitle = getProperty(news, 'newsTitle') || news.NewsTitle || 'Untitled';
            const createdDate = getProperty(news, 'createdDate') || news.CreatedDate || new Date();

            const date = new Date(createdDate).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric'
            });
            html += `
                <div class="related-news-item" onclick="showNewsDetail('${newsId}')">
                    <strong>${escapeHtml(newsTitle)}</strong><br>
                    <small class="text-muted"><i class="far fa-calendar"></i> ${date}</small>
                </div>
            `;
        });
        $('#relatedNewsList').html(html);
        $('#relatedNewsSection').show();
    } else {
        $('#relatedNewsSection').hide();
    }
}

// Show/hide loading
function showLoading(show) {
    if (show) {
        $('#loadingSpinner').show();
        $('#newsList').hide();
    } else {
        $('#loadingSpinner').hide();
        $('#newsList').show();
    }
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.toString().replace(/[&<>"']/g, function (m) { return map[m]; });
}

// Check login status
function checkLoginStatus() {
    const userInfo = sessionStorage.getItem('userInfo');
    if (userInfo) {
        try {
            const user = JSON.parse(userInfo);
            $('.btn-login').html(`<i class="fas fa-user"></i> ${user.name}`);
            $('.btn-login').attr('onclick', 'showUserMenu()');
            $('.btn-login').attr('href', '#');
        } catch (e) {
            sessionStorage.removeItem('userInfo');
        }
    }
}

function showUserMenu() {
    const userInfo = JSON.parse(sessionStorage.getItem('userInfo'));
    if (confirm(`Logged in as: ${userInfo.email}\n\nDo you want to logout?`)) {
        sessionStorage.removeItem('userInfo');
        location.reload();
    }
}