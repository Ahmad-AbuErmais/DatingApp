


SELECT count(*),Slug,Name ,Status, cultureId FROM `dcms-45`.pages_page group by Slug, CultureId,siteId Having count(*)>1;

Select * FROM `dcms-45`.pages_page where Slug Like 'test' and CultureId =  1;